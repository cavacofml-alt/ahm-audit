using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Auditorias
{
    public class CreateModel : PageModel
    {
        private readonly AuditDbContext _context;
        public CreateModel(AuditDbContext context) { _context = context; }

        [BindProperty]
        public Auditoria Auditoria { get; set; } = new();

        public List<string> Agents { get; set; } = new();
        public List<string> Officers { get; set; } = new();
        public List<string> RecentRegistrations { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            var username = HttpContext.Session.GetString("User");
            ViewData["IsAdmin"] = _context.Users.Any(u => u.Username == username && u.IsAdmin);

            LoadDropdowns();
            LoadRecentRegistrations();
            return Page();
        }

        public IActionResult OnPost()
        {
            LoadDropdowns();
            LoadRecentRegistrations();

            var username = HttpContext.Session.GetString("User");
            ViewData["IsAdmin"] = _context.Users.Any(u => u.Username == username && u.IsAdmin);

            if (string.IsNullOrWhiteSpace(Auditoria.Ticket))
            {
                ErrorMessage = "O Ticket Number é obrigatório.";
                return Page();
            }
            if (!Auditoria.Ticket.StartsWith("#"))
                Auditoria.Ticket = "#" + Auditoria.Ticket;

            if (_context.Auditorias.Any(a => a.Ticket == Auditoria.Ticket))
            {
                ErrorMessage = $"O Ticket '{Auditoria.Ticket}' já existe.";
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Auditoria.CorrectionTicket))
            {
                if (!Auditoria.CorrectionTicket.StartsWith("#"))
                    Auditoria.CorrectionTicket = "#" + Auditoria.CorrectionTicket;
                if (_context.Auditorias.Any(a => a.CorrectionTicket == Auditoria.CorrectionTicket))
                {
                    ErrorMessage = $"O Correction Ticket '{Auditoria.CorrectionTicket}' já existe.";
                    return Page();
                }
            }

            if (Auditoria.AircraftRecertified == "YES" && string.IsNullOrWhiteSpace(Auditoria.CorrectionTicket))
            {
                ErrorMessage = "O Correction Ticket é obrigatório quando o Aircraft é Re-certificado.";
                return Page();
            }

            if (!string.IsNullOrEmpty(Auditoria.Agent) && Auditoria.Agent == Auditoria.AhmOfficer)
            {
                ErrorMessage = "O Audit Agent não pode ser o mesmo que o AHM Officer.";
                return Page();
            }

            if (!string.IsNullOrEmpty(Auditoria.Agent) && Auditoria.Agent == username)
            {
                ErrorMessage = "O Audit Agent não pode auditar-se a si mesmo.";
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Auditoria.Airline))
                Auditoria.Airline = Auditoria.Airline.ToUpper();

            Auditoria.Agent                    = Auditoria.Agent                    ?? "";
            Auditoria.AhmOfficer               = Auditoria.AhmOfficer               ?? "";
            Auditoria.Airline                  = Auditoria.Airline                  ?? "";
            Auditoria.Aircraft                 = Auditoria.Aircraft                 ?? "";
            Auditoria.Registration             = Auditoria.Registration             ?? "";
            Auditoria.RevisionUpdates          = Auditoria.RevisionUpdates          ?? "";
            Auditoria.CorrectionTicket         = Auditoria.CorrectionTicket         ?? "";
            Auditoria.CorrectionsMade          = Auditoria.CorrectionsMade          ?? "N/A";
            Auditoria.AircraftRecertified      = Auditoria.AircraftRecertified      ?? "N/A";
            Auditoria.ReasonForRecertification = Auditoria.ReasonForRecertification ?? "";
            Auditoria.Notes                    = Auditoria.Notes                    ?? "";
            Auditoria.CreatedAt                = DateTime.Now;

            if (Auditoria.Date == default) Auditoria.Date = DateTime.Today;

            _context.Auditorias.Add(Auditoria);
            _context.SaveChanges();
            return RedirectToPage("Index");
        }

        private void LoadDropdowns()
        {
            Agents   = _context.Persons.Where(p => p.Role == "Agent"   && p.Active).Select(p => p.Name).OrderBy(n => n).ToList();
            Officers = _context.Persons.Where(p => p.Role == "Officer" && p.Active).Select(p => p.Name).OrderBy(n => n).ToList();
        }

        private void LoadRecentRegistrations()
        {
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            // Guardar combinações "AIRLINE|AIRCRAFT|REGISTRATION" para validação no frontend
            RecentRegistrations = _context.Auditorias
                .Where(a => a.Date >= sixMonthsAgo
                    && !string.IsNullOrEmpty(a.Registration)
                    && !string.IsNullOrEmpty(a.Airline)
                    && !string.IsNullOrEmpty(a.Aircraft))
                .Select(a => a.Airline + "|" + a.Aircraft + "|" + a.Registration)
                .Distinct().ToList();
        }
    }
}
