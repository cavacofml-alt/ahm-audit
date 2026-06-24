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
        public Auditoria Auditoria { get; set; }
        public List<string> Agents { get; set; } = new();
        public List<string> Officers { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            Auditoria = _context.Auditorias.Find(id);
            if (Auditoria == null) return RedirectToPage("Index");

            LoadDropdowns();
            return Page();
        }

        public IActionResult OnPost()
        {
            LoadDropdowns();

            Auditoria.Agent                    = Auditoria.Agent                    ?? "";
            Auditoria.AhmOfficer               = Auditoria.AhmOfficer               ?? "";
            Auditoria.Ticket                   = Auditoria.Ticket                   ?? "";
            Auditoria.Airline                  = Auditoria.Airline                  ?? "";
            Auditoria.Aircraft                 = Auditoria.Aircraft                 ?? "";
            Auditoria.Registration             = Auditoria.Registration             ?? "";
            Auditoria.RevisionUpdates          = Auditoria.RevisionUpdates          ?? "";
            Auditoria.CorrectionTicket         = Auditoria.CorrectionTicket         ?? "";
            Auditoria.ReasonForRecertification = Auditoria.ReasonForRecertification ?? "";
            Auditoria.Notes                    = Auditoria.Notes                    ?? "";

            _context.Auditorias.Update(Auditoria);
            _context.SaveChanges();
            return RedirectToPage("Index");
        }

        private void LoadDropdowns()
        {
            Agents   = _context.Persons.Where(p => p.Role == "Agent"   && p.Active).Select(p => p.Name).OrderBy(n => n).ToList();
            Officers = _context.Persons.Where(p => p.Role == "Officer" && p.Active).Select(p => p.Name).OrderBy(n => n).ToList();
        }
    }
}
