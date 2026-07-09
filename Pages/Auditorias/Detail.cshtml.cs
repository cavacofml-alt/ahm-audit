using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Reflection;

namespace AHM.Audit.Pages.Auditorias
{
    public class ChecklistItem { public string Label { get; set; } = ""; public string Value { get; set; } = "N/A"; public string? Reason { get; set; } }

    public class DetailModel : PageModel
    {
        private readonly AuditDbContext _context;
        public DetailModel(AuditDbContext context) { _context = context; }

        public Auditoria Auditoria { get; set; } = new();
        public List<ChecklistItem> ChecklistItems { get; set; } = new();
        public int CountYes { get; set; }
        public int CountNo  { get; set; }
        public int CountNA  { get; set; }

        private static readonly (string label, string field)[] Items = new[]
        {
            ("B1 - Standard units and codes",       "B1"),
            ("B2 - Crew and crew baggage weights",  "B2"),
            ("B3 - Passenger and baggage weights",  "B3"),
            ("C1 - Aircraft Type or fleet",         "C1"),
            ("C2 - Balance/Special info",           "C2"),
            ("C2.3 - Sup info",                     "C2_3"),
            ("C3 - Basic index/MAR RC Formula",     "C3"),
            ("C4.1 - Take-off",                     "C4_TakeOff"),
            ("C4.1 - Zero-Fuel",                    "C4_ZeroFuel"),
            ("C4.1 - Landing",                      "C4_Landing"),
            ("C4.1 - Inflight",                     "C4_Inflight"),
            ("C4.1 - Ideal Trim",                   "C4_IdealTrim"),
            ("C5 - Fuel",                           "C5"),
            ("C7.1 - Stab trim",                    "C7_1"),
            ("D1 - Dimensions and limits",          "D1"),
            ("D2 - Holds",                          "D2"),
            ("D3 - ULD",                            "D3"),
            ("D5.1 - Cabin",                        "D5_1"),
            ("D5.2 - Cabin Crew Seats",             "D5_2"),
            ("D6.2 - Seatmap",                      "D6_2"),
            ("E1 - DOW/DOI (BW/BI)",                "E1_DOW"),
            ("E1 - MRW",                            "E1_MRW"),
            ("E1 - MTOW",                           "E1_MTOW"),
            ("E1 - MZFW",                           "E1_MZFW"),
            ("E1 - MLAW",                           "E1_MLAW"),
            ("E2.1 - Crew Codes",                   "E2_1"),
            ("E2.2 - Crew Distribution",            "E2_2"),
            ("E3.1 - Pantry Distribution",          "E3_1"),
            ("G1 - ULD Compatibility",              "G1"),
            ("Revision update was correct?",        "RevisionUpdate"),
            ("LIR",                                 "LIR"),
            ("LS",                                  "LS"),
            ("Database Printout",                   "DatabasePrintout"),
        };

        public IActionResult OnGet(int id)
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            var username = HttpContext.Session.GetString("User");
            ViewData["IsAdmin"] = _context.Users.Any(u => u.Username == username && u.IsAdmin);

            Auditoria = _context.Auditorias.Find(id);
            if (Auditoria == null) return RedirectToPage("Index");

            // Extrai o dicionário campo->razão de Auditoria.NoReasons ("campo=razão;...")
            var noReasons = string.IsNullOrEmpty(Auditoria.NoReasons)
                ? new Dictionary<string, string>()
                : Auditoria.NoReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split('=', 2))
                    .Where(p => p.Length == 2)
                    .GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last()[1]);

            foreach (var (label, field) in Items)
            {
                var val = typeof(Auditoria).GetProperty(field)?.GetValue(Auditoria)?.ToString() ?? "N/A";
                var reason = val == "NO" ? noReasons.GetValueOrDefault(field) : null;
                ChecklistItems.Add(new ChecklistItem { Label = label, Value = val, Reason = reason });
                if (val == "YES") CountYes++;
                else if (val == "NO") CountNo++;
                else CountNA++;
            }
            return Page();
        }
    }
}
