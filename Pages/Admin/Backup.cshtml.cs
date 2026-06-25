using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Text;
using System.Globalization;

namespace AHM.Audit.Pages.Admin
{
    [IgnoreAntiforgeryToken]
    public class BackupModel : PageModel
    {
        private readonly AuditDbContext _context;
        public BackupModel(AuditDbContext context) { _context = context; }

        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        public bool IsAdmin { get; set; }

        public IActionResult OnGet()
        {
            if (!CheckAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            IsAdmin = true;
            return Page();
        }

        public IActionResult OnPostImport(IFormFile csvFile)
        {
            if (!CheckAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            IsAdmin = true;

            if (csvFile == null || csvFile.Length == 0)
            {
                Message = "Seleciona um ficheiro CSV."; IsError = true; return Page();
            }

            int imported = 0, skipped = 0;

            using var reader = new System.IO.StreamReader(csvFile.OpenReadStream());
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = ParseCsvLine(line);
                if (cols.Length < 10) continue;

                var ticket = cols[0].Trim();
                if (string.IsNullOrEmpty(ticket)) continue;

                if (_context.Auditorias.Any(a => a.Ticket == ticket)) { skipped++; continue; }

                try
                {
                    var dateStr = SafeGet(cols, 6);
                    DateTime parsedDate = DateTime.Today;
                    DateTime.TryParseExact(
                        dateStr,
                        new[] { "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out parsedDate);

                    var a = new Auditoria
                    {
                        Ticket                   = ticket,
                        Agent                    = SafeGet(cols, 1),
                        AhmOfficer               = SafeGet(cols, 2),
                        Airline                  = SafeGet(cols, 3),
                        Aircraft                 = SafeGet(cols, 4),
                        Registration             = SafeGet(cols, 5),
                        Date                     = parsedDate == default ? DateTime.Today : parsedDate,
                        RevisionUpdates          = SafeGet(cols, 7),
                        B1  = SafeGet(cols, 8),  B2  = SafeGet(cols, 9),  B3  = SafeGet(cols, 10),
                        C1  = SafeGet(cols, 11), C2  = SafeGet(cols, 12), C2_3 = SafeGet(cols, 13),
                        C3  = SafeGet(cols, 14), C4_TakeOff = SafeGet(cols, 15), C4_ZeroFuel = SafeGet(cols, 16),
                        C4_Landing = SafeGet(cols, 17), C4_Inflight = SafeGet(cols, 18), C4_IdealTrim = SafeGet(cols, 19),
                        C5  = SafeGet(cols, 20), C7_1 = SafeGet(cols, 21),
                        D1  = SafeGet(cols, 22), D2  = SafeGet(cols, 23), D3  = SafeGet(cols, 24),
                        D5_1 = SafeGet(cols, 25), D5_2 = SafeGet(cols, 26), D6_2 = SafeGet(cols, 27),
                        E1_DOW = SafeGet(cols, 28), E1_MRW = SafeGet(cols, 29), E1_MTOW = SafeGet(cols, 30),
                        E1_MZFW = SafeGet(cols, 31), E1_MLAW = SafeGet(cols, 32),
                        E2_1 = SafeGet(cols, 33), E2_2 = SafeGet(cols, 34), E3_1 = SafeGet(cols, 35),
                        G1  = SafeGet(cols, 36), RevisionUpdate = SafeGet(cols, 37),
                        LIR = SafeGet(cols, 38), LS  = SafeGet(cols, 39), DatabasePrintout = SafeGet(cols, 40),
                        CorrectionTicket         = SafeGet(cols, 41),
                        ReasonForRecertification = SafeGet(cols, 42),
                        CorrectionsMade          = SafeGet(cols, 43),
                        AircraftRecertified      = SafeGet(cols, 44),
                        Notes                    = SafeGet(cols, 45),
                        CreatedAt                = DateTime.Now
                    };
                    _context.Auditorias.Add(a);
                    imported++;
                }
                catch { skipped++; }
            }

            _context.SaveChanges();
            Message = $"Importação concluída: {imported} auditoria(s) importada(s), {skipped} ignorada(s).";
            return Page();
        }

        public IActionResult OnPostDeleteAll()
        {
            if (!CheckAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            IsAdmin = true;

            var all = _context.Auditorias.ToList();
            _context.Auditorias.RemoveRange(all);
            _context.SaveChanges();

            Message = $"{all.Count} auditoria(s) apagada(s) com sucesso.";
            return Page();
        }

        private string SafeGet(string[] arr, int i) =>
            i < arr.Length ? arr[i].Trim('"', ' ') : "N/A";

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; }
                else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        private bool CheckAdmin()
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return false;
            return _context.Users.Any(u => u.Username == username && u.IsAdmin);
        }
    }
}
