using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Reflection;
using System.Text;

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

            var audits = _context.Auditorias.OrderByDescending(a => a.CreatedAt).ToList();
            var sb = new StringBuilder();

            sb.AppendLine("Ticket,Agent,AHM Officer,Airline,Aircraft,Registration,Date,Revision Updates," +
                          "B1,B2,B3,C1,C2,C2.3,C3,C4-TakeOff,C4-ZeroFuel,C4-Landing,C4-Inflight,C4-IdealTrim,C5,C7.1," +
                          "D1,D2,D3,D5.1,D5.2,D6.2,E1-DOW,E1-MRW,E1-MTOW,E1-MZFW,E1-MLAW,E2.1,E2.2,E3.1," +
                          "G1,RevisionUpdate,LIR,LS,DatabasePrintout," +
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

                var row = new[] { Escape(a.Ticket), Escape(a.Agent), Escape(a.AhmOfficer),
                    Escape(a.Airline), Escape(a.Aircraft), Escape(a.Registration),
                    a.Date.ToString("dd/MM/yyyy"), Escape(a.RevisionUpdates) }
                    .Concat(values)
                    .Concat(new[] { Escape(a.CorrectionTicket), Escape(a.ReasonForRecertification),
                        a.CorrectionsMade, a.AircraftRecertified, Escape(a.Notes),
                        yes.ToString(), no.ToString(), na.ToString(), pct + "%" });

                sb.AppendLine(string.Join(",", row));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"AHM_Auditorias_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private string Escape(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            return val;
        }
    }
}
