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
        public List<string> NonConformityReasons { get; set; } = new();
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

            NonConformityReasons = _context.NonConformityReasons.Where(r => r.Active).Select(r => r.Reason).ToList();
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
            NonConformityReasons = _context.NonConformityReasons.Where(r => r.Active).Select(r => r.Reason).ToList();

            // Nota: os campos da checklist e as razões de NO (NoReasons) NÃO são copiados aqui —
            // são guardados diretamente na BD via /api/autosave assim que o utilizador os altera
            // (ver Edit.cshtml). Copiar o modelo inteiro apagaria as razões já guardadas, porque
            // este formulário não reenvia esses campos.
            original.Agent                    = Auditoria.Agent                    ?? "";
            original.AhmOfficer               = Auditoria.AhmOfficer               ?? "";
            original.Ticket                   = Auditoria.Ticket                   ?? "";
            original.Airline                  = Auditoria.Airline                  ?? "";
            original.Aircraft                 = Auditoria.Aircraft                 ?? "";
            original.Registration             = Auditoria.Registration             ?? "";
            original.Date                      = Auditoria.Date;
            original.RevisionUpdates          = Auditoria.RevisionUpdates          ?? "";
            original.CorrectionTicket         = Auditoria.CorrectionTicket         ?? "";
            original.CorrectionsMade          = Auditoria.CorrectionsMade          ?? "N/A";
            original.AircraftRecertified      = Auditoria.AircraftRecertified      ?? "N/A";
            original.ReasonForRecertification = Auditoria.ReasonForRecertification ?? "";
            original.Notes                    = Auditoria.Notes                    ?? "";

            // Finalizar ou manter rascunho
            original.IsFinalized = action == "finalize";

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
