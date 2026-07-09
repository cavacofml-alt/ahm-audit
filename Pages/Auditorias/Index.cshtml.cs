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

        public string? FilterAirline { get; set; }
        public string? FilterNcField { get; set; }
        public string? FilterOfficer { get; set; }
        public string? FilterNcReason { get; set; }
        public string? FilterCompliance { get; set; }

        public IActionResult OnGet(string? airline, string? ncFilter, string? officer, string? ncReason, string? compliance)
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            var username = HttpContext.Session.GetString("User");
            IsAdmin = _context.Users.Any(u => u.Username == username && u.IsAdmin);
            ViewData["IsAdmin"] = IsAdmin;

            FilterAirline    = airline;
            FilterNcField    = ncFilter;
            FilterOfficer    = officer;
            FilterNcReason   = ncReason;
            FilterCompliance = compliance;

            var query = _context.Auditorias.AsQueryable();

            if (!string.IsNullOrEmpty(airline))
                query = query.Where(a => a.Airline == airline);

            if (!string.IsNullOrEmpty(officer))
                query = query.Where(a => a.AhmOfficer == officer);

            var audits = query.OrderByDescending(a => a.CreatedAt).ToList();

            // Filtra pelo doughnut "Conformidade global" do dashboard: "ok" = auditorias sem
            // nenhum item NOT OK; "notok" = auditorias com pelo menos um item NOT OK.
            if (compliance == "ok")
                audits = audits.Where(a => CountNo(a) == 0).ToList();
            else if (compliance == "notok")
                audits = audits.Where(a => CountNo(a) > 0).ToList();

            // Filtra por razão de NO pré-definida (selecionada a partir do gráfico "Razões de NOT OK"
            // ou do drill-down por Officer no dashboard)
            if (!string.IsNullOrEmpty(ncReason))
            {
                audits = audits.Where(a =>
                    !string.IsNullOrEmpty(a.NoReasons) &&
                    a.NoReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Split('=', 2))
                        .Any(p => p.Length == 2 && p[1] == ncReason)
                ).ToList();
            }

            // Filter by NC field if specified (field label → show audits where that field = NO)
            if (!string.IsNullOrEmpty(ncFilter))
            {
                var fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"B1 - Standard units","B1"},{"B2 - Crew baggage","B2"},{"B3 - Pax baggage","B3"},
                    {"C1 - Aircraft Type","C1"},{"C2 - Balance/Special","C2"},{"C2.3 - Sup info","C2_3"},
                    {"C3 - Basic index","C3"},{"C4.1 - Take-off","C4_TakeOff"},{"C4.1 - Zero-Fuel","C4_ZeroFuel"},
                    {"C4.1 - Landing","C4_Landing"},{"C4.1 - Inflight","C4_Inflight"},{"C4.1 - Ideal Trim","C4_IdealTrim"},
                    {"C5 - Fuel","C5"},{"C7.1 - Stab trim","C7_1"},{"D1 - Dimensions","D1"},
                    {"D2 - Holds","D2"},{"D3 - ULD","D3"},{"D5.1 - Cabin","D5_1"},
                    {"D5.2 - Cabin Crew Seats","D5_2"},{"D6.2 - Seatmap","D6_2"},
                    {"E1 - DOW/DOI","E1_DOW"},{"E1 - MRW","E1_MRW"},{"E1 - MTOW","E1_MTOW"},
                    {"E1 - MZFW","E1_MZFW"},{"E1 - MLAW","E1_MLAW"},
                    {"E2.1 - Crew Codes","E2_1"},{"E2.2 - Crew Distribution","E2_2"},
                    {"E3.1 - Pantry","E3_1"},{"G1 - ULD Compatibility","G1"},
                    {"Revision update","RevisionUpdate"},{"LIR","LIR"},{"LS","LS"},{"Database Printout","DatabasePrintout"}
                };

                if (fieldMap.TryGetValue(ncFilter, out var fieldName))
                {
                    audits = audits.Where(a =>
                        typeof(Auditoria).GetProperty(fieldName)?.GetValue(a)?.ToString() == "NO"
                    ).ToList();
                }
            }

            Auditorias = audits;
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

        // Prazo máximo para finalizar uma auditoria: 2 semanas a partir da criação.
        public const int FinalizationDeadlineDays = 14;

        public DateTime GetDeadline(Auditoria a) => a.CreatedAt.AddDays(FinalizationDeadlineDays);

        // Dias que faltam até ao prazo (negativo = já passou o prazo). Só faz sentido
        // para auditorias ainda não finalizadas.
        public int DaysUntilDeadline(Auditoria a) => (GetDeadline(a).Date - DateTime.UtcNow.Date).Days;
    }
}
