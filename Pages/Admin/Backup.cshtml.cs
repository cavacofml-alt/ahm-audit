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
            var validReasons  = _context.NonConformityReasons.Where(r => r.Active).Select(r => r.Reason).ToList();
            // Tickets já existentes na BD + tickets já vistos neste próprio ficheiro (para não
            // deixar passar duplicados dentro do mesmo CSV, já que só há um SaveChanges no fim).
            var existingTickets = _context.Auditorias.Select(a => a.Ticket).ToHashSet();
            var seenInThisFile  = new HashSet<string>();

            // Lê o ficheiro todo para memória primeiro, para se poder validar tudo antes de
            // gravar qualquer coisa — se houver um nome de Agent/Officer não reconhecido, ou
            // uma razão de NO não reconhecida/em falta, em qualquer linha, a importação inteira
            // falha e nada é gravado.
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
            const int checklistStartCol = 8; // primeira coluna da checklist (B1) no CSV

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

                // Valida a razão de cada item marcado NO (formato "NO (razão)" dentro da célula).
                // Um NO sem razão nenhuma é aceite (fica "sem razão registada", como já acontece
                // nas páginas de detalhe) — só bloqueia se a razão vier escrita mas não corresponder
                // a nenhuma das predefinidas (proteção contra erros de escrita).
                for (int i = 0; i < AuditCsvExporter.ChecklistFields.Length; i++)
                {
                    var field = AuditCsvExporter.ChecklistFields[i];
                    var (val, reason) = ParseChecklistCell(SafeGet(cols, checklistStartCol + i));
                    if (val != "NO" || string.IsNullOrEmpty(reason)) continue;

                    if (!validReasons.Any(r => NormalizeName(r) == NormalizeName(reason)))
                    {
                        nameErrors.Add($"Linha {rowNum} (ticket {ticket}): razão '{reason}' do item '{field}' não existe em Admin > Definições.");
                    }
                }

                rowsToImport.Add((cols, ticket, agentMatch ?? agentRaw, officerMatch ?? officerRaw));
            }

            if (nameErrors.Any())
            {
                Message = "Importação cancelada — nenhuma auditoria foi gravada. Corrige o CSV (nomes e razões têm de ser exatamente iguais aos que existem em Admin > Pessoas / Admin > Definições) ou adiciona-os lá primeiro:\n"
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

                    // Extrai só o valor (YES/NO/N-A) de cada célula da checklist, e reconstrói o
                    // campo NoReasons ("campo=razão;campo2=razão2") a partir das razões já validadas.
                    var checklistValues = new Dictionary<string, string>();
                    var noReasonsParts = new List<string>();
                    for (int i = 0; i < AuditCsvExporter.ChecklistFields.Length; i++)
                    {
                        var field = AuditCsvExporter.ChecklistFields[i];
                        var (val, reason) = ParseChecklistCell(SafeGet(cols, checklistStartCol + i));
                        checklistValues[field] = val;
                        if (val == "NO" && !string.IsNullOrEmpty(reason))
                        {
                            var matchedReason = validReasons.FirstOrDefault(r => NormalizeName(r) == NormalizeName(reason)) ?? reason;
                            noReasonsParts.Add($"{field}={matchedReason}");
                        }
                    }

                    var a = new Auditoria
                    {
                        Ticket = ticket, Agent = agent, AhmOfficer = officer,
                        Airline = SafeGet(cols, 3), Aircraft = SafeGet(cols, 4),
                        Registration = SafeGet(cols, 5),
                        Date = parsedDate == default ? DateTime.Today : parsedDate,
                        RevisionUpdates = SafeGet(cols, 7),
                        B1=checklistValues["B1"], B2=checklistValues["B2"], B3=checklistValues["B3"],
                        C1=checklistValues["C1"], C2=checklistValues["C2"], C2_3=checklistValues["C2_3"],
                        C3=checklistValues["C3"], C4_TakeOff=checklistValues["C4_TakeOff"], C4_ZeroFuel=checklistValues["C4_ZeroFuel"],
                        C4_Landing=checklistValues["C4_Landing"], C4_Inflight=checklistValues["C4_Inflight"], C4_IdealTrim=checklistValues["C4_IdealTrim"],
                        C5=checklistValues["C5"], C7_1=checklistValues["C7_1"],
                        D1=checklistValues["D1"], D2=checklistValues["D2"], D3=checklistValues["D3"],
                        D5_1=checklistValues["D5_1"], D5_2=checklistValues["D5_2"], D6_2=checklistValues["D6_2"],
                        E1_DOW=checklistValues["E1_DOW"], E1_MRW=checklistValues["E1_MRW"], E1_MTOW=checklistValues["E1_MTOW"],
                        E1_MZFW=checklistValues["E1_MZFW"], E1_MLAW=checklistValues["E1_MLAW"],
                        E2_1=checklistValues["E2_1"], E2_2=checklistValues["E2_2"], E3_1=checklistValues["E3_1"],
                        G1=checklistValues["G1"], RevisionUpdate=checklistValues["RevisionUpdate"],
                        LIR=checklistValues["LIR"], LS=checklistValues["LS"], DatabasePrintout=checklistValues["DatabasePrintout"],
                        NoReasons = string.Join(";", noReasonsParts),
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

        // Extrai o valor (YES/NO/N-A) e a razão opcional de uma célula da checklist no formato
        // que o "Exportar CSV" produz, ex.: "NO (Information incomplete in AHM)".
        private static (string value, string? reason) ParseChecklistCell(string raw)
        {
            raw = (raw ?? "").Trim();
            var m = System.Text.RegularExpressions.Regex.Match(
                raw, @"^(YES|NO|N/A)\s*(?:\((.*)\))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return (raw, null);
            var value = m.Groups[1].Value.ToUpperInvariant();
            var reason = m.Groups[2].Success ? m.Groups[2].Value.Trim() : null;
            return (value, reason);
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
