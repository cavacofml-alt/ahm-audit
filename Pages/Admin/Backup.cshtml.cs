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
            // uma razão de NO não reconhecida, em qualquer linha, a importação inteira falha e
            // nada é gravado.
            //
            // IMPORTANTE: lê-se o ficheiro inteiro e processa-se com um parser de CSV a sério
            // (ParseCsv), não linha a linha — células com texto em várias linhas (ex.: notas de
            // correção longas) partiam uma única linha de dados em várias com StreamReader.ReadLine(),
            // desalinhando todas as colunas a partir daí.
            string csvContent;
            using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
            {
                csvContent = reader.ReadToEnd();
            }
            var allRows = ParseCsv(csvContent);
            var dataRows = allRows.Skip(1) // salta o cabeçalho
                .Where(r => r.Count > 1 || (r.Count == 1 && !string.IsNullOrWhiteSpace(r[0])))
                .ToList();

            var nameErrors = new List<string>();
            var skippedDetails = new List<string>();
            var rowsToImport = new List<(string[] cols, string ticket, string agent, string officer)>();
            int rowNum = 1; // linha 1 = cabeçalho, por isso as linhas de dados começam em 2
            const int checklistStartCol = 8; // primeira coluna da checklist (B1) no CSV

            foreach (var row in dataRows)
            {
                rowNum++;
                var cols = row.ToArray();
                if (cols.Length < 10) { skippedDetails.Add($"Linha {rowNum}: menos colunas do que o esperado."); continue; }
                var ticket = cols[0].Trim();
                if (string.IsNullOrEmpty(ticket)) { skippedDetails.Add($"Linha {rowNum}: sem número de ticket."); continue; }
                if (existingTickets.Contains(ticket)) { skippedDetails.Add($"Linha {rowNum}: ticket {ticket} já existe na base de dados."); continue; }
                if (!seenInThisFile.Add(ticket)) { skippedDetails.Add($"Linha {rowNum}: ticket {ticket} repetido dentro do próprio ficheiro."); continue; }

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

            int imported = 0;

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
                        CreatedAt=DateTime.UtcNow
                    };
                    _context.Auditorias.Add(a);
                    imported++;
                }
                catch (Exception ex) { skippedDetails.Add($"Ticket {ticket}: erro ao processar a linha ({ex.Message})."); }
            }

            _context.SaveChanges();
            Message = $"Importação concluída: {imported} auditoria(s) importada(s), {skippedDetails.Count} ignorada(s).";
            if (skippedDetails.Count > 0)
                Message += "\n" + string.Join("\n", skippedDetails.Take(30))
                    + (skippedDetails.Count > 30 ? $"\n... e mais {skippedDetails.Count - 30}." : "");
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

        // Parser de CSV a sério: lê o conteúdo inteiro do ficheiro (não linha a linha), respeitando
        // aspas — incluindo quando uma célula tem texto com quebras de linha lá dentro (ex.: notas
        // de correção longas), que com leitura linha a linha partiam uma linha de dados em várias
        // e desalinhavam todas as colunas a partir daí.
        private static List<List<string>> ParseCsv(string content)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int i = 0;
            int n = content.Length;

            while (i < n)
            {
                char c = content[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < n && content[i + 1] == '"') { field.Append('"'); i += 2; continue; }
                        inQuotes = false; i++; continue;
                    }
                    field.Append(c); i++; continue;
                }

                if (c == '"') { inQuotes = true; i++; continue; }
                if (c == ',') { currentRow.Add(field.ToString()); field.Clear(); i++; continue; }
                if (c == '\r') { i++; continue; }
                if (c == '\n')
                {
                    currentRow.Add(field.ToString()); field.Clear();
                    rows.Add(currentRow); currentRow = new List<string>();
                    i++; continue;
                }
                field.Append(c); i++;
            }

            if (field.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(field.ToString());
                rows.Add(currentRow);
            }
            return rows;
        }

        private bool CheckAdmin()
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return false;
            return _context.Users.Any(u => u.Username == username && u.IsAdmin);
        }
    }
}
