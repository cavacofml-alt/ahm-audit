using AHM.Audit.Models;
using ClosedXML.Excel;

namespace AHM.Audit.Data
{
    /// <summary>
    /// Gera o .xlsx do export de auditorias, partilhado por Pages/Auditorias/Export e
    /// Pages/Admin/BackupExport — mesma ideia do AuditCsvExporter, mas para o formato
    /// que a equipa já usava antes de haver aplicação (cada auditoria é uma coluna, os
    /// campos ficam fixos nas linhas da coluna A). Traz só os campos que o AHM Audit
    /// regista hoje — campos do ficheiro antigo sem equivalente aqui (ex. "Pedro's
    /// review", "Employee ID") ficam de fora, por decisão do utilizador (2026-10-21):
    /// não há dados reais para pôr lá.
    /// </summary>
    public static class AuditXlsxExporter
    {
        // Mesmos campos e rótulos do checklist já usados no CSV (AuditCsvExporter), na mesma
        // ordem de AuditCsvExporter.ChecklistFields — tem de vir antes de "Rows" abaixo, que a
        // usa já na sua própria inicialização (ordem de inicialização de campos estáticos).
        private static readonly string[] ChecklistLabels =
        {
            "B1 - Standard units and codes","B2 - Crew and crew baggage weights",
            "B3 - Passenger and baggage weights","C1 - Aircraft Type or fleet",
            "C2 - Balance/Special info","C2.3 - Sup info","C3 - Basic index/MAR RC Formula",
            "C4.1 - Take-off","C4.1 - Zero-Fuel","C4.1 - Landing","C4.1 - Inflight",
            "C4.1 - Ideal Trim","C5 - Fuel","C7.1 - Stab trim","D1 - Dimensions and limits",
            "D2 - Holds","D3 - ULD","D5.1 - Cabin","D5.2 - Cabin Crew Seats","D6.2 - Seatmap",
            "E1 - DOW/DOI (BW/BI)","E1 - MRW","E1 - MTOW","E1 - MZFW","E1 - MLAW",
            "E2.1 - Crew Codes","E2.2 - Crew Distribution","E3.1 - Pantry Distribution",
            "G1 - ULD Compatibility","Revision update was correct?","LIR","LS","Database Printout"
        };

        // Quantas linhas fixas (Agent, Ticket, ...) vêm antes do checklist em BuildRows() —
        // usado para saber a que item da checklist corresponde cada linha de Rows.
        private const int FixedRowsCount = 8;

        // (rótulo na coluna A, como ir buscar o valor a cada auditoria)
        private static readonly (string Label, Func<Auditoria, Dictionary<string, string>, object?> Value)[] Rows = BuildRows();

        public static byte[] BuildXlsx(IEnumerable<Auditoria> audits)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("AHM Audit");

            for (int r = 0; r < Rows.Length; r++)
            {
                var cell = ws.Cell(r + 1, 1);
                cell.Value = Rows[r].Label;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Column(1).Width = 35;

            int col = 2;
            foreach (var a in audits)
            {
                var reasons = string.IsNullOrEmpty(a.NoReasons)
                    ? new Dictionary<string, string>()
                    : a.NoReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Split('=', 2))
                        .Where(p => p.Length == 2)
                        .ToDictionary(p => p[0], p => p[1]);

                // Observações de texto livre por item da checklist (Auditoria.ItemObservations,
                // JSON {"C3":"texto",...}) — não têm linha própria no layout; vão como comentário
                // na célula do item a que pertencem (fixedRows.Length = 8 linhas antes do checklist).
                var itemObservations = string.IsNullOrEmpty(a.ItemObservations)
                    ? new Dictionary<string, string>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.ItemObservations) ?? new();

                for (int r = 0; r < Rows.Length; r++)
                {
                    var cell = ws.Cell(r + 1, col);
                    var value = Rows[r].Value(a, reasons);
                    if (value is DateTime dt)
                    {
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = "dd/mm/yyyy";
                    }
                    else
                    {
                        cell.Value = value?.ToString() ?? "";
                    }
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var checklistIndex = r - FixedRowsCount;
                    if (checklistIndex >= 0 && checklistIndex < ChecklistLabels.Length)
                    {
                        var field = AuditCsvExporter.ChecklistFields[checklistIndex];
                        if (itemObservations.TryGetValue(field, out var obsText) && !string.IsNullOrWhiteSpace(obsText))
                            cell.CreateComment().AddText(obsText);
                    }
                }
                ws.Column(col).Width = 20;
                col++;
            }

            ws.SheetView.FreezeColumns(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static (string, Func<Auditoria, Dictionary<string, string>, object?>)[] BuildRows()
        {
            var fixedRows = new (string, Func<Auditoria, Dictionary<string, string>, object?>)[]
            {
                ("Audit Agent", (a, _) => a.Agent),
                ("Ticket", (a, _) => a.Ticket),
                ("Audit Date", (a, _) => a.Date),
                ("Airline", (a, _) => a.Airline),
                ("Aircraft Type", (a, _) => a.Aircraft),
                ("Registration", (a, _) => a.Registration),
                ("Rev. Updates", (a, _) => a.RevisionUpdates),
                ("AHM Officer", (a, _) => a.AhmOfficer),
            };

            var checklistRows = new (string, Func<Auditoria, Dictionary<string, string>, object?>)[ChecklistLabels.Length];
            for (int i = 0; i < ChecklistLabels.Length; i++)
            {
                var field = AuditCsvExporter.ChecklistFields[i];
                checklistRows[i] = (ChecklistLabels[i], (a, reasons) =>
                {
                    var val = typeof(Auditoria).GetProperty(field)?.GetValue(a)?.ToString() ?? "N/A";
                    if (val == "NO" && reasons.TryGetValue(field, out var reason) && !string.IsNullOrEmpty(reason))
                        return $"NO ({reason})";
                    return val;
                });
            }

            var trailingRows = new (string, Func<Auditoria, Dictionary<string, string>, object?>)[]
            {
                ("Notes / impact / and reason for possible mismatch", (a, _) => a.Notes),
                ("Corrections Made", (a, _) => a.CorrectionsMade),
                ("Aircraft Re-Certified", (a, _) => a.AircraftRecertified),
                ("Worked on/ correction Ticket", (a, _) => a.CorrectionTicket),
                ("Reason for Re-Certification", (a, _) => a.ReasonForRecertification),
                ("Total NOT OK (N)", (a, _) => FormatCountWithPct(a, "NO")),
                ("Total OK (Y)", (a, _) => FormatCountWithPct(a, "YES")),
            };

            return fixedRows.Concat(checklistRows).Concat(trailingRows).ToArray();
        }

        private static int CountChecklist(Auditoria a, string match) =>
            AuditCsvExporter.ChecklistFields.Count(f =>
                (typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A") == match);

        // "12 (34%)" — a contagem seguida da percentagem sobre Y+N entre parêntesis, mesma
        // fórmula do dashboard (Pages/Index.cshtml.cs): ignora N/A. Sem nenhum item Y/N
        // marcado, fica só a contagem (sem percentagem, para não dividir por 0).
        private static string FormatCountWithPct(Auditoria a, string match)
        {
            var count = CountChecklist(a, match);
            var total = CountChecklist(a, "YES") + CountChecklist(a, "NO");
            return total == 0 ? count.ToString() : $"{count} ({count * 100 / total}%)";
        }

        /// <summary>
        /// Lê um .xlsx gerado por BuildXlsx (ou com a mesma estrutura — coluna A com os
        /// rótulos, uma coluna por auditoria) e devolve linhas no mesmo formato que
        /// Backup.cshtml.cs já sabe processar a partir do ParseCsv de AuditCsvExporter:
        /// uma linha de cabeçalho (ignorada, só para o .Skip(1) continuar a funcionar
        /// sem alterações) seguida de uma linha por auditoria, com os campos nos mesmos
        /// índices 0-45 do CSV antigo (Ticket, Agent, Officer, Airline, Aircraft,
        /// Registration, Date, RevisionUpdates, 33 campos da checklist, CorrectionTicket,
        /// ReasonForRecertification, CorrectionsMade, AircraftRecertified, Notes) — para
        /// o resto da importação (validação de nomes/razões, tudo-ou-nada) não precisar
        /// de saber se o ficheiro era CSV ou Excel.
        /// </summary>
        /// <summary>
        /// Overload sem observações, para quem não precisa delas (ex.: testes antigos).
        /// </summary>
        public static List<List<string>> ReadToCsvRows(Stream xlsxStream) =>
            ReadToCsvRows(xlsxStream, out _);

        /// <summary>
        /// itemObservations sai com a mesma ordem/comprimento do resultado (uma entrada por linha,
        /// incluindo a linha 0 de cabeçalho, sempre vazia) — o dicionário campo→texto de cada
        /// auditoria, lido dos comentários nas células da checklist que BuildXlsx escreveu.
        /// </summary>
        public static List<List<string>> ReadToCsvRows(Stream xlsxStream, out List<Dictionary<string, string>> itemObservations)
        {
            itemObservations = new List<Dictionary<string, string>> { new() }; // linha 0 = cabeçalho
            using var wb = new XLWorkbook(xlsxStream);
            var ws = wb.Worksheets.First();

            // Linha (1-based) de cada rótulo da coluna A — os mesmos rótulos que BuildXlsx escreve.
            var rowByLabel = new Dictionary<string, int>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 1; r <= lastRow; r++)
            {
                var label = ws.Cell(r, 1).GetString();
                if (!string.IsNullOrEmpty(label)) rowByLabel[label] = r;
            }

            string GetField(string label, int col) =>
                rowByLabel.TryGetValue(label, out var r) ? GetCellAsCsvString(ws.Cell(r, col)) : "";

            var result = new List<List<string>> { new() }; // linha 0 = cabeçalho (ignorado)

            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;
            for (int col = 2; col <= lastCol; col++)
            {
                if (!rowByLabel.TryGetValue("Ticket", out var ticketRow) ||
                    string.IsNullOrWhiteSpace(ws.Cell(ticketRow, col).GetString()))
                    continue; // coluna vazia (ex. a seguir à última auditoria)

                var cols = new List<string>
                {
                    GetField("Ticket", col),
                    GetField("Audit Agent", col),
                    GetField("AHM Officer", col),
                    GetField("Airline", col),
                    GetField("Aircraft Type", col),
                    GetField("Registration", col),
                    GetField("Audit Date", col),
                    GetField("Rev. Updates", col),
                };
                for (int i = 0; i < 33; i++)
                    cols.Add(GetField(ChecklistLabels[i], col));
                cols.Add(GetField("Worked on/ correction Ticket", col));
                cols.Add(GetField("Reason for Re-Certification", col));
                cols.Add(GetField("Corrections Made", col));
                cols.Add(GetField("Aircraft Re-Certified", col));
                cols.Add(GetField("Notes / impact / and reason for possible mismatch", col));

                result.Add(cols);

                var obs = new Dictionary<string, string>();
                for (int i = 0; i < ChecklistLabels.Length; i++)
                {
                    if (!rowByLabel.TryGetValue(ChecklistLabels[i], out var r)) continue;
                    var cell = ws.Cell(r, col);
                    if (cell.HasComment)
                    {
                        var text = cell.GetComment().Text?.Trim();
                        if (!string.IsNullOrEmpty(text))
                            obs[AuditCsvExporter.ChecklistFields[i]] = text;
                    }
                }
                itemObservations.Add(obs);
            }
            return result;
        }

        // Datas ficam como texto dd/MM/yyyy (um dos formatos que Backup.cshtml.cs já aceita);
        // tudo o resto sai como texto normal da célula.
        private static string GetCellAsCsvString(IXLCell cell) =>
            cell.DataType == XLDataType.DateTime
                ? cell.GetDateTime().ToString("dd/MM/yyyy")
                : cell.GetString();
    }
}
