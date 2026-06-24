using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Reflection;

namespace AHM.Audit.Pages.Auditorias
{
    public class ArchiveModel : PageModel
    {
        private readonly AuditDbContext _context;
        public ArchiveModel(AuditDbContext context) { _context = context; }

        public List<int> AvailableYears { get; set; } = new();
        public List<AuditoriaArchive> Auditorias { get; set; } = new();
        public int SelectedYear { get; set; }

        public IActionResult OnGet(int year = 0)
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            var username = HttpContext.Session.GetString("User");
            ViewData["IsAdmin"] = _context.Users.Any(u => u.Username == username && u.IsAdmin);

            AvailableYears = _context.AuditoriaArchives
                .Select(a => a.ArchiveYear).Distinct().OrderByDescending(y => y).ToList();

            SelectedYear = year;
            if (year > 0)
                Auditorias = _context.AuditoriaArchives
                    .Where(a => a.ArchiveYear == year)
                    .OrderByDescending(a => a.Date)
                    .ToList();

            return Page();
        }

        private static readonly string[] ChecklistFields = new[]
        {
            "B1","B2","B3","C1","C2","C2_3","C3","C4_TakeOff","C4_ZeroFuel",
            "C4_Landing","C4_Inflight","C4_IdealTrim","C5","C7_1","D1","D2",
            "D3","D5_1","D5_2","D6_2","E1_DOW","E1_MRW","E1_MTOW","E1_MZFW",
            "E1_MLAW","E2_1","E2_2","E3_1","G1","RevisionUpdate","LIR","LS","DatabasePrintout"
        };

        private IEnumerable<string> GetValues(AuditoriaArchive a) =>
            ChecklistFields.Select(f => typeof(AuditoriaArchive).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A");

        public int CountYes(AuditoriaArchive a) => GetValues(a).Count(v => v == "YES");
        public int CountNo(AuditoriaArchive a)  => GetValues(a).Count(v => v == "NO");
        public int CountNA(AuditoriaArchive a)  => GetValues(a).Count(v => v == "N/A");
    }
}
