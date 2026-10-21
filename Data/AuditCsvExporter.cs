using AHM.Audit.Models;
using System.Text;

namespace AHM.Audit.Data
{
    /// <summary>
    /// Gera o CSV de auditorias partilhado por Pages/Auditorias/Export e Pages/Admin/BackupExport.
    /// Antes existiam duas cópias quase idênticas deste código — qualquer alteração a um campo
    /// tinha de ser replicada manualmente nos dois sítios. Agora há uma única fonte da verdade.
    /// </summary>
    public static class AuditCsvExporter
    {
        public static readonly string[] ChecklistFields = new[]
        {
            "B1","B2","B3","C1","C2","C2_3","C3","C4_TakeOff","C4_ZeroFuel",
            "C4_Landing","C4_Inflight","C4_IdealTrim","C5","C7_1","D1","D2",
            "D3","D5_1","D5_2","D6_2","E1_DOW","E1_MRW","E1_MTOW","E1_MZFW",
            "E1_MLAW","E2_1","E2_2","E3_1","G1","RevisionUpdate","LIR","LS","DatabasePrintout"
        };

        private const string Header =
            "Ticket,Agent,AHM Officer,Airline,Aircraft,Registration,Date,Revision Updates," +
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
            "CorrectionTicket,ReasonForRecertification,CorrectionsMade,AircraftRecertified,Notes,YES,NO,N/A,Conformidade%";

        /// <summary>
        /// Constrói o CSV a partir da lista de auditorias fornecida. Não filtra nada por si só —
        /// quem chama decide, por exemplo, se quer incluir rascunhos ou não.
        /// </summary>
        public static byte[] BuildCsv(IEnumerable<Auditoria> audits)
        {
            var sb = new StringBuilder();
            // CRLF explícito, não AppendLine (que usa Environment.NewLine — "\n" no Linux,
            // onde esta app corre): tem de ser sempre diferente de uma quebra de linha
            // dentro de um campo (ex. "Notes" com várias linhas, ver CollapseBlankLines),
            // senão o Excel não consegue distinguir "nota com 2 linhas" de "próxima
            // auditoria" e parte a linha de dados em várias, desalinhando tudo a seguir —
            // bug real, reproduzido a exportar uma auditoria com notas em várias linhas.
            const string NewLine = "\r\n";
            sb.Append(Header).Append(NewLine);

            foreach (var a in audits)
            {
                // Extrai o dicionário campo->razão de NoReasons ("campo=razão;campo2=razão2")
                var reasons = string.IsNullOrEmpty(a.NoReasons)
                    ? new Dictionary<string, string>()
                    : a.NoReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Split('=', 2))
                        .Where(p => p.Length == 2)
                        .ToDictionary(p => p[0], p => p[1]);

                var values = ChecklistFields.Select(f =>
                {
                    var val = typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A";
                    // A razão fica dentro da própria célula (ex.: "NO (Information incomplete
                    // in AHM)"), em vez de precisar de 33 colunas extra só para as razões —
                    // fica editável no Excel e a importação sabe ler este formato de volta.
                    if (val == "NO" && reasons.TryGetValue(f, out var reason) && !string.IsNullOrEmpty(reason))
                        return $"NO ({reason})";
                    return val;
                }).ToList();

                int yes = values.Count(v => v == "YES");
                int no  = values.Count(v => v == "NO" || v.StartsWith("NO ("));
                int na  = values.Count(v => v == "N/A");
                int pct = (yes + no) > 0 ? yes * 100 / (yes + no) : 0;

                var row = new[] { Escape(a.Ticket), Escape(a.Agent), Escape(a.AhmOfficer),
                    Escape(a.Airline), Escape(a.Aircraft), Escape(a.Registration),
                    a.Date.ToString("dd/MM/yyyy"), Escape(a.RevisionUpdates) }
                    .Concat(values.Select(Escape))
                    .Concat(new[] { Escape(a.CorrectionTicket), Escape(a.ReasonForRecertification),
                        a.CorrectionsMade, a.AircraftRecertified, Escape(CollapseBlankLines(a.Notes)),
                        yes.ToString(), no.ToString(), na.ToString(), pct + "%" });

                sb.Append(string.Join(",", row)).Append(NewLine);
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        private static string Escape(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            // Proteção contra injeção de fórmulas: um valor que comece por =, +, -, @, tab ou
            // carriage return é interpretado como fórmula ao abrir o CSV no Excel — um apóstrofo
            // à frente força o Excel a tratá-lo como texto. A importação (Backup.cshtml.cs,
            // SafeGet) tira este apóstrofo de volta ao ler um CSV exportado por aqui.
            if (val[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
                val = "'" + val;
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            return val;
        }

        // Reduz sequências de várias linhas em branco (ex.: "texto\r\n\r\n\r\n\r\noutro texto")
        // a uma só quebra de linha — deixa a célula "Notes" mais compacta e legível no Excel.
        private static string? CollapseBlankLines(string? val)
        {
            if (string.IsNullOrEmpty(val)) return val;
            return System.Text.RegularExpressions.Regex.Replace(val, @"(\r\n|\r|\n){2,}", "\n");
        }
    }
}
