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

            // Mesmos campos e rótulos do checklist já usados no CSV (AuditCsvExporter),
            // só que aqui cada um vira uma linha em vez de uma coluna.
            var checklistLabels = new[]
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

            var checklistRows = new (string, Func<Auditoria, Dictionary<string, string>, object?>)[checklistLabels.Length];
            for (int i = 0; i < checklistLabels.Length; i++)
            {
                var field = AuditCsvExporter.ChecklistFields[i];
                checklistRows[i] = (checklistLabels[i], (a, reasons) =>
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
                ("Total NOT OK (N)", (a, reasons) => CountChecklist(a, "NO")),
                ("Total OK (Y)", (a, reasons) => CountChecklist(a, "YES")),
            };

            return fixedRows.Concat(checklistRows).Concat(trailingRows).ToArray();
        }

        private static int CountChecklist(Auditoria a, string match) =>
            AuditCsvExporter.ChecklistFields.Count(f =>
                (typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A") == match);
    }
}
