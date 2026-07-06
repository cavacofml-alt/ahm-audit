using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;

namespace AHM.Audit.Pages.Admin
{
    public class BackupExportModel : PageModel
    {
        private readonly AuditDbContext _context;
        public BackupExportModel(AuditDbContext context) { _context = context; }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");
            if (!_context.Users.Any(u => u.Username == username && u.IsAdmin)) return Forbid();

            // Este é o backup completo do admin (ao contrário de Auditorias/Export, que é para
            // consulta geral): inclui de propósito também os rascunhos, para ser uma cópia
            // fiel de tudo o que está na base de dados.
            var audits = _context.Auditorias.OrderByDescending(a => a.CreatedAt).ToList();

            var bytes = AuditCsvExporter.BuildCsv(audits);
            return File(bytes, "text/csv", $"AHM_Backup_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}
