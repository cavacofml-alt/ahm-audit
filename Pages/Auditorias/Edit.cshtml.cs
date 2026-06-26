using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Auditorias
{
    public class EditModel : PageModel
    {
        private readonly AuditDbContext _context;
        public EditModel(AuditDbContext context) { _context = context; }

        [BindProperty]
        public Auditoria Auditoria { get; set; } = new();
        public List<string> Agents { get; set; } = new();
        public List<string> Officers { get; set; } = new();
        public bool IsAdmin { get; set; }
        public bool IsLocked { get; set; }

        public IActionResult OnGet(int id)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            IsAdmin = _context.Users.Any(u => u.Username == username && u.IsAdmin);
            ViewData["IsAdmin"] = IsAdmin;

            Auditoria = _context.Auditorias.Find(id);
            if (Auditoria == null) return RedirectToPage("Index");

            // Bloqueado se finalizado (não admin) ou passou mais de 1 mês (não admin)
            IsLocked = !IsAdmin && (Auditoria.IsFinalized || Auditoria.CreatedAt < DateTime.Now.AddMonths(-1));

            LoadDropdowns();
            return Page();
        }

        public IActionResult OnPost(string? action)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            var isAdmin = _context.Users.Any(u => u.Username == username && u.IsAdmin);
            ViewData["IsAdmin"] = isAdmin;
            IsAdmin = isAdmin;

            var original = _context.Auditorias.Find(Auditoria.Id);
            if (original == null) return RedirectToPage("Index");

            // Verificar bloqueio
            if (!isAdmin && (original.IsFinalized || original.CreatedAt < DateTime.Now.AddMonths(-1)))
            {
                IsLocked = true;
                Auditoria = original;
                LoadDropdowns();
                ModelState.AddModelError("", "Esta auditoria está bloqueada.");
                return Page();
            }

            LoadDropdowns();

            Auditoria.Agent                    = Auditoria.Agent                    ?? "";
            Auditoria.AhmOfficer               = Auditoria.AhmOfficer               ?? "";
            Auditoria.Ticket                   = Auditoria.Ticket                   ?? "";
            Auditoria.Airline                  = Auditoria.Airline                  ?? "";
            Auditoria.Aircraft                 = Auditoria.Aircraft                 ?? "";
            Auditoria.Registration             = Auditoria.Registration             ?? "";
            Auditoria.RevisionUpdates          = Auditoria.RevisionUpdates          ?? "";
            Auditoria.CorrectionTicket         = Auditoria.CorrectionTicket         ?? "";
            Auditoria.CorrectionsMade          = Auditoria.CorrectionsMade          ?? "N/A";
            Auditoria.AircraftRecertified      = Auditoria.AircraftRecertified      ?? "N/A";
            Auditoria.ReasonForRecertification = Auditoria.ReasonForRecertification ?? "";
            Auditoria.Notes                    = Auditoria.Notes                    ?? "";
            Auditoria.CreatedAt                = original.CreatedAt;

            // Finalizar ou manter rascunho
            Auditoria.IsFinalized = action == "finalize";

            _context.Entry(original).CurrentValues.SetValues(Auditoria);
            _context.SaveChanges();
            return RedirectToPage("Index");
        }

        public IActionResult OnPostUnfinalize(int id)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");
            if (!_context.Users.Any(u => u.Username == username && u.IsAdmin)) return Forbid();

            var audit = _context.Auditorias.Find(id);
            if (audit != null)
            {
                audit.IsFinalized = false;
                _context.SaveChanges();
            }
            return RedirectToPage(new { id });
        }

        private void LoadDropdowns()
        {
            Agents   = _context.Persons.Where(p => p.Role == "Agent"   && p.Active).Select(p => p.Name).OrderBy(n => n).ToList();
            Officers = _context.Persons.Where(p => p.Role == "Officer" && p.Active).Select(p => p.Name).OrderBy(n => n).ToList();
        }
    }
}
