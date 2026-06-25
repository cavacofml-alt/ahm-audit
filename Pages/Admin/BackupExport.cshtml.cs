using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Reflection;
using System.Text;

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

            var audits = _context.Auditorias.OrderByDescending(a => a.CreatedAt).ToList();
            var sb = new StringBuilder();

            // Cabeçalhos com nomes completos da checklist
            sb.AppendLine("Ticket,Agent,AHM Officer,Airline,Aircraft,Registration,Date,Revision Updates," +
                          "B1 - Standard units and codes," +
                          "B2 - Crew and crew baggage weights," +
                          "B3 - Passenger and baggage weights," +
                          "C1 - Aircraft Type or fleet," +
                          "C2 - Balance/Special info," +
                          "C2.3 - Sup info," +
                          "C3 - Basic index/MAR RC Formula," +
                          "C4.1 - Take-off," +
                          "C4.1 - Zero-Fuel," +
                          "C4.1 - Landing," +
                          "C4.1 - Inflight," +
                          "C4.1 - Ideal Trim," +
                          "C5 - Fuel," +
                          "C7.1 - Stab trim," +
                          "D1 - Dimensions and limits," +
                          "D2 - Holds," +
                          "D3 - ULD," +
                          "D5.1 - Cabin," +
                          "D5.2 - Cabin Crew Seats," +
                          "D6.2 - Seatmap," +
                          "E1 - DOW/DOI (BW/BI)," +
                          "E1 - MRW," +
                          "E1 - MTOW," +
                          "E1 - MZFW," +
                          "E1 - MLAW," +
                          "E2.1 - Crew Codes," +
                          "E2.2 - Crew Distribution," +
                          "E3.1 - Pantry Distribution," +
                          "G1 - ULD Compatibility," +
                          "Revision update was correct?," +
                          "LIR," +
                          "LS," +
                          "Database Printout," +
                          "CorrectionTicket,ReasonForRecertification,CorrectionsMade,AircraftRecertified,Notes,YES,NO,N/A,Conformidade%");

            var checklistFields = new[]
            {
                "B1","B2","B3","C1","C2","C2_3","C3","C4_TakeOff","C4_ZeroFuel",
                "C4_Landing","C4_Inflight","C4_IdealTrim","C5","C7_1","D1","D2",
                "D3","D5_1","D5_2","D6_2","E1_DOW","E1_MRW","E1_MTOW","E1_MZFW",
                "E1_MLAW","E2_1","E2_2","E3_1","G1","RevisionUpdate","LIR","LS","DatabasePrintout"
            };

            foreach (var a in audits)
            {
                var values = checklistFields.Select(f =>
                    typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A").ToList();
                int yes = values.Count(v => v == "YES");
                int no  = values.Count(v => v == "NO");
                int na  = values.Count(v => v == "N/A");
                int pct = (yes + no + na) > 0 ? yes * 100 / (yes + no + na) : 0;

                var row = new[] { Esc(a.Ticket), Esc(a.Agent), Esc(a.AhmOfficer),
                    Esc(a.Airline), Esc(a.Aircraft), Esc(a.Registration),
                    a.Date.ToString("dd/MM/yyyy"), Esc(a.RevisionUpdates) }
                    .Concat(values)
                    .Concat(new[] { Esc(a.CorrectionTicket), Esc(a.ReasonForRecertification),
                        a.CorrectionsMade, a.AircraftRecertified, Esc(a.Notes),
                        yes.ToString(), no.ToString(), na.ToString(), pct + "%" });

                sb.AppendLine(string.Join(",", row));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"AHM_Backup_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private string Esc(string? v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            return v;
        }
    }
}
