using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;

namespace AHM.Audit.Pages.Auditorias
{
    public class ExportModel : PageModel
    {
        private readonly AuditDbContext _context;
        public ExportModel(AuditDbContext context) { _context = context; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            // Exclui rascunhos por finalizar — um export não deve incluir auditorias a meio.
            var audits = _context.Auditorias
                .Where(a => !a.IsDraft)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            var bytes = AuditCsvExporter.BuildCsv(audits);
            return File(bytes, "text/csv", $"AHM_Auditorias_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}
