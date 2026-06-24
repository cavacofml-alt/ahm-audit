using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace AHM.Audit.Pages.Auditorias
{
    public class IndexModel : PageModel
    {
        private readonly AuditDbContext _context;
        public IndexModel(AuditDbContext context) { _context = context; }

        public List<Auditoria> Auditorias { get; set; } = new();
        public bool IsAdmin { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            var username = HttpContext.Session.GetString("User");
            IsAdmin = _context.Users.Any(u => u.Username == username && u.IsAdmin);
            ViewData["IsAdmin"] = IsAdmin;

            Auditorias = _context.Auditorias.OrderByDescending(a => a.CreatedAt).ToList();
            return Page();
        }

        public IActionResult OnPostDeleteSelected(string ids)
        {
            var username = HttpContext.Session.GetString("User");
            var isAdmin = _context.Users.Any(u => u.Username == username && u.IsAdmin);
            if (!isAdmin) return Forbid();

            if (!string.IsNullOrEmpty(ids))
            {
                var idList = ids.Split(',').Select(s => int.TryParse(s.Trim(), out var n) ? n : 0).Where(n => n > 0).ToList();
                var toDelete = _context.Auditorias.Where(a => idList.Contains(a.Id)).ToList();
                _context.Auditorias.RemoveRange(toDelete);
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        private static readonly string[] ChecklistFields = new[]
        {
            "B1","B2","B3","C1","C2","C2_3","C3","C4_TakeOff","C4_ZeroFuel",
            "C4_Landing","C4_Inflight","C4_IdealTrim","C5","C7_1","D1","D2",
            "D3","D5_1","D5_2","D6_2","E1_DOW","E1_MRW","E1_MTOW","E1_MZFW",
            "E1_MLAW","E2_1","E2_2","E3_1","G1","RevisionUpdate","LIR","LS","DatabasePrintout"
        };

        private IEnumerable<string> GetValues(Auditoria a) =>
            ChecklistFields.Select(f => typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A");

        public int CountYes(Auditoria a) => GetValues(a).Count(v => v == "YES");
        public int CountNo(Auditoria a)  => GetValues(a).Count(v => v == "NO");
        public int CountNA(Auditoria a)  => GetValues(a).Count(v => v == "N/A");
    }
}
