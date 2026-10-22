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
            // StatusCode(403), não Forbid() — Forbid() precisa de um esquema de autenticação
            // registado (app.AddAuthentication), que esta app não tem (sessão simples); sem
            // isto rebenta com uma exceção em vez de dar um 403 limpo.
            if (!_context.Users.Any(u => u.Username == username && u.IsAdmin)) return StatusCode(403);

            // Este é o backup completo do admin (ao contrário de Auditorias/Export, que é para
            // consulta geral): inclui de propósito também os rascunhos, para ser uma cópia
            // fiel de tudo o que está na base de dados.
            var audits = _context.Auditorias.OrderByDescending(a => a.CreatedAt).ToList();

            var bytes = AuditXlsxExporter.BuildXlsx(audits);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"AHM_Backup_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }
    }
}
