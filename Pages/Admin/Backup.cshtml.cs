using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Text;
using System.Globalization;

namespace AHM.Audit.Pages.Admin
{
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

            var validAgents   = _context.Persons.Where(p => p.Role == "Agent").Select(p => p.Name).ToList();
            var validOfficers = _context.Persons.Where(p => p.Role == "Officer").Select(p => p.Name).ToList();
            // Tickets já existentes na BD + tickets já vistos neste próprio ficheiro (para não
            // deixar passar duplicados dentro do mesmo CSV, já que só há um SaveChanges no fim).
            var existingTickets = _context.Auditorias.Select(a => a.Ticket).ToHashSet();
            var seenInThisFile  = new HashSet<string>();

            // Lê o ficheiro todo para memória primeiro, para se poder validar tudo antes de
            // gravar qualquer coisa — se houver um nome de Agent/Officer não reconhecido em
            // qualquer linha, a importação inteira falha e nada é gravado.
            var lines = new List<string>();
            using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
            {
                reader.ReadLine(); // cabeçalho
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
                }
            }

            var nameErrors = new List<string>();
            var rowsToImport = new List<(string[] cols, string ticket, string agent, string officer)>();
            int rowNum = 1; // linha 1 = cabeçalho, por isso as linhas de dados começam em 2

            foreach (var line in lines)
            {
                rowNum++;
                var cols = ParseCsvLine(line);
                if (cols.Length < 10) continue;
                var ticket = cols[0].Trim();
                if (string.IsNullOrEmpty(ticket)) continue;
                if (existingTickets.Contains(ticket) || !seenInThisFile.Add(ticket)) continue;

                var agentRaw   = SafeGet(cols, 1).Trim();
                var officerRaw = SafeGet(cols, 2).Trim();

                // Compara ignorando acentos/maiúsculas (ex.: "Joao" == "João" na BD).
                var agentMatch   = validAgents.FirstOrDefault(p => NormalizeName(p) == NormalizeName(agentRaw));
                var officerMatch = validOfficers.FirstOrDefault(p => NormalizeName(p) == NormalizeName(officerRaw));

                if (!string.IsNullOrEmpty(agentRaw) && agentMatch == null)
                    nameErrors.Add($"Linha {rowNum} (ticket {ticket}): Agent '{agentRaw}' não existe em Admin > Pessoas.");
                if (!string.IsNullOrEmpty(officerRaw) && officerMatch == null)
                    nameErrors.Add($"Linha {rowNum} (ticket {ticket}): AHM Officer '{officerRaw}' não existe em Admin > Pessoas.");

                rowsToImport.Add((cols, ticket, agentMatch ?? agentRaw, officerMatch ?? officerRaw));
            }

            if (nameErrors.Any())
            {
                Message = "Importação cancelada — nenhuma auditoria foi gravada. Corrige os nomes no CSV (têm de ser exatamente iguais aos que existem em Admin > Pessoas) ou adiciona-os lá primeiro:\n"
                    + string.Join("\n", nameErrors.Take(20))
                    + (nameErrors.Count > 20 ? $"\n... e mais {nameErrors.Count - 20} problema(s)." : "");
                IsError = true;
                return Page();
            }

            int imported = 0, skipped = lines.Count - rowsToImport.Count;

            foreach (var (cols, ticket, agent, officer) in rowsToImport)
            {
                try
                {
                    var dateStr = SafeGet(cols, 6);
                    DateTime parsedDate = DateTime.Today;
                    DateTime.TryParseExact(dateStr,
                        new[] { "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);

                    var a = new Auditoria
                    {
                        Ticket = ticket, Agent = agent, AhmOfficer = officer,
                        Airline = SafeGet(cols, 3), Aircraft = SafeGet(cols, 4),
                        Registration = SafeGet(cols, 5),
                        Date = parsedDate == default ? DateTime.Today : parsedDate,
                        RevisionUpdates = SafeGet(cols, 7),
                        B1=SafeGet(cols,8), B2=SafeGet(cols,9), B3=SafeGet(cols,10),
                        C1=SafeGet(cols,11), C2=SafeGet(cols,12), C2_3=SafeGet(cols,13),
                        C3=SafeGet(cols,14), C4_TakeOff=SafeGet(cols,15), C4_ZeroFuel=SafeGet(cols,16),
                        C4_Landing=SafeGet(cols,17), C4_Inflight=SafeGet(cols,18), C4_IdealTrim=SafeGet(cols,19),
                        C5=SafeGet(cols,20), C7_1=SafeGet(cols,21),
                        D1=SafeGet(cols,22), D2=SafeGet(cols,23), D3=SafeGet(cols,24),
                        D5_1=SafeGet(cols,25), D5_2=SafeGet(cols,26), D6_2=SafeGet(cols,27),
                        E1_DOW=SafeGet(cols,28), E1_MRW=SafeGet(cols,29), E1_MTOW=SafeGet(cols,30),
                        E1_MZFW=SafeGet(cols,31), E1_MLAW=SafeGet(cols,32),
                        E2_1=SafeGet(cols,33), E2_2=SafeGet(cols,34), E3_1=SafeGet(cols,35),
                        G1=SafeGet(cols,36), RevisionUpdate=SafeGet(cols,37),
                        LIR=SafeGet(cols,38), LS=SafeGet(cols,39), DatabasePrintout=SafeGet(cols,40),
                        CorrectionTicket=SafeGet(cols,41),
                        ReasonForRecertification=SafeGet(cols,42),
                        CorrectionsMade=SafeGet(cols,43),
                        AircraftRecertified=SafeGet(cols,44),
                        Notes=SafeGet(cols,45),
                        CreatedAt=DateTime.Now
                    };
                    _context.Auditorias.Add(a);
                    imported++;
                }
                catch { skipped++; }
            }

            _context.SaveChanges();
            Message = $"Importação concluída: {imported} auditoria(s) importada(s), {skipped} ignorada(s) (ticket em falta/duplicado).";
            return Page();
        }

        // Remove acentos e normaliza para comparação de nomes tolerante a diferenças de escrita
        // (ex.: "Joao Viriato" == "João Viriato").
        private static string NormalizeName(string name)
        {
            var normalized = name.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
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

        public IActionResult OnPostDeleteArchive()
        {
            if (!CheckAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            IsAdmin = true;
            var all = _context.AuditoriaArchives.ToList();
            _context.AuditoriaArchives.RemoveRange(all);
            _context.SaveChanges();
            Message = $"{all.Count} registo(s) de arquivo apagado(s) com sucesso.";
            return Page();
        }

        private string SafeGet(string[] arr, int i) =>
            i < arr.Length ? arr[i].Trim('"', ' ') : "";

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
