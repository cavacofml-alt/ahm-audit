using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace AHM.Audit.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuditDbContext _context;
        public IndexModel(AuditDbContext context) { _context = context; }

        public bool IsAdmin { get; set; }
        public bool CanViewNonConformities  { get; set; } = true;
        public bool CanViewGlobalConformity { get; set; } = true;
        public bool CanViewTrend            { get; set; } = true;
        public bool CanViewHeatmap          { get; set; } = true;
        public bool CanViewAirlineChart     { get; set; } = true;
        public bool CanViewAgentChart       { get; set; } = true;
        public bool CanViewOfficerChart     { get; set; } = true;
        public bool CanViewComparativeChart { get; set; } = true;
        public bool CanViewQuarterProgress  { get; set; } = true;

        // KPIs
        public int Total        { get; set; }
        public int TotalYes     { get; set; }
        public int TotalNo      { get; set; }
        public int TotalNA      { get; set; }
        public string PctYes    { get; set; } = "0";
        public string PctNo     { get; set; } = "0";
        public string PctNA     { get; set; } = "0";
        public string ScoreGrade { get; set; } = "N/A";

        // Comparação com período anterior
        public int PrevTotal        { get; set; }
        public string PrevPctYes    { get; set; } = "0";
        public int DiffTotal        { get; set; }
        public int DiffPctYes       { get; set; }
        public int CompareYear      { get; set; }
        public int CurrentDataYear  { get; set; }

        // KPI mês atual vs anterior
        public int ThisMonthTotal   { get; set; }
        public int LastMonthTotal   { get; set; }
        public int ThisMonthNC      { get; set; }
        public int LastMonthNC      { get; set; }
        public int ThisMonthPct     { get; set; }
        public int LastMonthPct     { get; set; }
        public string LastAuditTime { get; set; } = "-";

        // Filtros
        public DateTime? FilterFrom { get; set; }
        public DateTime? FilterTo   { get; set; }
        public string? FilterAirline { get; set; }
        public string? FilterAgent   { get; set; }
        public int CurrentQuarter    { get; set; }
        public int CurrentYear       { get; set; }

        public List<string> AllAirlines { get; set; } = new();
        public List<string> AllAgents   { get; set; } = new();

        public List<QuarterStat>       QuarterProgress      { get; set; } = new();
        public List<string>            AirlineLabels        { get; set; } = new();
        public List<int>               AirlineValues        { get; set; } = new();
        public List<string>            AgentLabels          { get; set; } = new();
        public List<int>               AgentValues          { get; set; } = new();
        public List<string>            OfficerLabels        { get; set; } = new();
        public List<int>               OfficerConformity    { get; set; } = new();
        public List<SectionStat>       SectionData          { get; set; } = new();
        public List<string>            SectionNames         { get; set; } = new();
        public List<ItemNonConformity> TopNonConformities   { get; set; } = new();
        public List<MonthTrend>        MonthlyTrend         { get; set; } = new();
        public List<ChecklistFieldDetail> ChecklistDetail   { get; set; } = new();
        public Dictionary<string, OfficerStat> OfficerStats { get; set; } = new();

        // Razões de NO (gráfico global) e drill-down por Officer
        public List<ReasonStat> ReasonStats { get; set; } = new();
        public Dictionary<string, List<ReasonStat>> OfficerReasonDrill { get; set; } = new();

        // Extrai o dicionário campo->razão do texto guardado em Auditoria.NoReasons ("campo=razão;...")
        private static Dictionary<string, string> ParseNoReasons(string? noReasons)
        {
            if (string.IsNullOrEmpty(noReasons)) return new Dictionary<string, string>();
            return noReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);
        }

        private static List<ReasonStat> CountReasons(IEnumerable<Auditoria> audits, int take = 10)
        {
            var counts = new Dictionary<string, int>();
            foreach (var a in audits)
                foreach (var reason in ParseNoReasons(a.NoReasons).Values)
                    counts[reason] = counts.GetValueOrDefault(reason) + 1;

            return counts
                .OrderByDescending(kv => kv.Value)
                .Take(take)
                .Select(kv => new ReasonStat { Reason = kv.Key, Count = kv.Value })
                .ToList();
        }


        private static readonly (string field, string label, string sectionKey, string sectionName)[] ChecklistItems = new[]
        {
            ("B1",  "B1 - Standard units",          "B", "B - Standard/Crew/Pax Weights"),
            ("B2",  "B2 - Crew baggage",             "B", "B - Standard/Crew/Pax Weights"),
            ("B3",  "B3 - Pax baggage",              "B", "B - Standard/Crew/Pax Weights"),
            ("C1",  "C1 - Aircraft Type",            "C", "C - Balance & Fuel"),
            ("C2",  "C2 - Balance/Special",          "C", "C - Balance & Fuel"),
            ("C2_3","C2.3 - Sup info",               "C", "C - Balance & Fuel"),
            ("C3",  "C3 - Basic index",              "C", "C - Balance & Fuel"),
            ("C4_TakeOff",  "C4.1 - Take-off",      "C", "C - Balance & Fuel"),
            ("C4_ZeroFuel", "C4.1 - Zero-Fuel",     "C", "C - Balance & Fuel"),
            ("C4_Landing",  "C4.1 - Landing",        "C", "C - Balance & Fuel"),
            ("C4_Inflight", "C4.1 - Inflight",       "C", "C - Balance & Fuel"),
            ("C4_IdealTrim","C4.1 - Ideal Trim",     "C", "C - Balance & Fuel"),
            ("C5",  "C5 - Fuel",                    "C", "C - Balance & Fuel"),
            ("C7_1","C7.1 - Stab trim",              "C", "C - Balance & Fuel"),
            ("D1",  "D1 - Dimensions",              "D", "D - Dimensions & Cabin"),
            ("D2",  "D2 - Holds",                   "D", "D - Dimensions & Cabin"),
            ("D3",  "D3 - ULD",                     "D", "D - Dimensions & Cabin"),
            ("D5_1","D5.1 - Cabin",                 "D", "D - Dimensions & Cabin"),
            ("D5_2","D5.2 - Cabin Crew Seats",      "D", "D - Dimensions & Cabin"),
            ("D6_2","D6.2 - Seatmap",               "D", "D - Dimensions & Cabin"),
            ("E1_DOW", "E1 - DOW/DOI",              "E", "E - DOW/Crew/Pantry"),
            ("E1_MRW", "E1 - MRW",                  "E", "E - DOW/Crew/Pantry"),
            ("E1_MTOW","E1 - MTOW",                 "E", "E - DOW/Crew/Pantry"),
            ("E1_MZFW","E1 - MZFW",                 "E", "E - DOW/Crew/Pantry"),
            ("E1_MLAW","E1 - MLAW",                 "E", "E - DOW/Crew/Pantry"),
            ("E2_1","E2.1 - Crew Codes",            "E", "E - DOW/Crew/Pantry"),
            ("E2_2","E2.2 - Crew Distribution",     "E", "E - DOW/Crew/Pantry"),
            ("E3_1","E3.1 - Pantry",                "E", "E - DOW/Crew/Pantry"),
            ("G1",  "G1 - ULD Compatibility",       "G", "G - ULD Compatibility"),
            ("RevisionUpdate","Revision update",    "H", "H - Administrative"),
            ("LIR", "LIR",                          "H", "H - Administrative"),
            ("LS",  "LS",                           "H", "H - Administrative"),
            ("DatabasePrintout","Database Printout","H", "H - Administrative"),
        };

        private (int yes, int no, int na) CountChecklist(List<Auditoria> audits)
        {
            int y = 0, n = 0, a = 0;
            foreach (var audit in audits)
                foreach (var (f, _, _, _) in ChecklistItems)
                {
                    var val = typeof(Auditoria).GetProperty(f)?.GetValue(audit)?.ToString() ?? "";
                    if (val == "YES") y++;
                    else if (val == "NO") n++;
                    else if (val == "N/A") a++;
                }
            return (y, n, a);
        }

        private string GetGrade(int pct) => pct >= 98 ? "A+" : pct >= 95 ? "A" : pct >= 90 ? "B" : pct >= 80 ? "C" : "D";

        public IActionResult OnGet(DateTime? from, DateTime? to, string? airline, string? agent)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToPage("/Account/Login");

            IsAdmin = user.IsAdmin;
            ViewData["IsAdmin"] = IsAdmin;

            if (!IsAdmin && !user.CanViewDashboard)
                return RedirectToPage("/Auditorias/Index");

            // Nota: ao contrário do acesso à página do dashboard (que os admins têm sempre,
            // acima), a visibilidade de cada gráfico respeita sempre o que está configurado
            // no modal de permissões — incluindo para admins. Um "IsAdmin ||" aqui faria com
            // que desmarcar um gráfico para um admin não tivesse qualquer efeito.
            CanViewNonConformities  = user.CanViewNonConformities;
            CanViewGlobalConformity = user.CanViewGlobalConformity;
            CanViewTrend            = user.CanViewTrend;
            CanViewHeatmap          = user.CanViewHeatmap;
            CanViewAirlineChart     = user.CanViewAirlineChart;
            CanViewAgentChart       = user.CanViewAgentChart;
            CanViewOfficerChart     = user.CanViewOfficerChart;
            CanViewComparativeChart = user.CanViewComparativeChart;
            CanViewQuarterProgress  = user.CanViewQuarterProgress;

            FilterFrom   = from;
            FilterTo     = to;
            FilterAirline = airline;
            FilterAgent   = agent;

            var now = DateTime.Now;
            CurrentQuarter = (now.Month - 1) / 3 + 1;
            CurrentYear    = now.Year;

            // Filter dropdowns
            AllAirlines = _context.Auditorias.Select(a => a.Airline).Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();
            AllAgents   = _context.Auditorias.Select(a => a.Agent).Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();

            // Quarter progress
            for (int q = 1; q <= 4; q++)
            {
                var qStart = new DateTime(now.Year, (q - 1) * 3 + 1, 1);
                var qEnd   = qStart.AddMonths(3);
                QuarterProgress.Add(new QuarterStat
                {
                    Label = $"Q{q} {now.Year}",
                    Count = _context.Auditorias.Count(a => a.Date >= qStart && a.Date < qEnd)
                });
            }

            // Main query with filters
            var query = _context.Auditorias.AsQueryable();
            if (from.HasValue)    query = query.Where(a => a.Date >= from.Value);
            if (to.HasValue)      query = query.Where(a => a.Date <= to.Value);
            if (!string.IsNullOrEmpty(airline)) query = query.Where(a => a.Airline == airline);
            if (!string.IsNullOrEmpty(agent))   query = query.Where(a => a.Agent == agent);
            var audits = query.ToList();

            Total = audits.Count;
            var (cy, cn, ca) = CountChecklist(audits);
            TotalYes = cy; TotalNo = cn; TotalNA = ca;

            int grand = TotalYes + TotalNo;
            if (grand > 0)
            {
                int pctInt = TotalYes * 100 / grand;
                PctYes = pctInt.ToString();
                PctNo  = (TotalNo * 100 / grand).ToString();
                PctNA  = TotalNA.ToString();
                ScoreGrade = GetGrade(pctInt);
            }

            // KPI this month vs last month
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var thisMonthAudits = _context.Auditorias.Where(a => a.Date >= thisMonthStart).ToList();
            var lastMonthAudits = _context.Auditorias.Where(a => a.Date >= lastMonthStart && a.Date < thisMonthStart).ToList();

            ThisMonthTotal = thisMonthAudits.Count;
            LastMonthTotal = lastMonthAudits.Count;

            var (tmy, tmn, _) = CountChecklist(thisMonthAudits);
            var (lmy, lmn, _) = CountChecklist(lastMonthAudits);
            ThisMonthPct = (tmy + tmn) > 0 ? tmy * 100 / (tmy + tmn) : 0;
            LastMonthPct = (lmy + lmn) > 0 ? lmy * 100 / (lmy + lmn) : 0;
            ThisMonthNC  = tmn;
            LastMonthNC  = lmn;

            // Last audit time
            var lastAudit = _context.Auditorias.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
            if (lastAudit != null)
            {
                var diff = DateTime.Now - lastAudit.CreatedAt;
                if (diff.TotalMinutes < 60) LastAuditTime = $"há {(int)diff.TotalMinutes} min";
                else if (diff.TotalHours < 24) LastAuditTime = $"há {(int)diff.TotalHours}h";
                else LastAuditTime = $"há {(int)diff.TotalDays}d";
            }

            // Previous year comparison
            var dataYear    = from.HasValue ? from.Value.Year : now.Year;
            CurrentDataYear = dataYear;
            CompareYear     = dataYear - 1;
            var prevAudits  = _context.AuditoriaArchives.Where(a => a.ArchiveYear == CompareYear).ToList();
            PrevTotal       = prevAudits.Count;
            if (prevAudits.Any())
            {
                int py = 0, pn = 0;
                foreach (var a in prevAudits)
                    foreach (var (f, _, _, _) in ChecklistItems)
                    {
                        var val = typeof(AuditoriaArchive).GetProperty(f)?.GetValue(a)?.ToString() ?? "";
                        if (val == "YES") py++;
                        else if (val == "NO") pn++;
                    }
                int prevPct = (py + pn) > 0 ? py * 100 / (py + pn) : 0;
                PrevPctYes  = prevPct.ToString();
                DiffPctYes  = int.Parse(PctYes) - prevPct;
                DiffTotal   = Total - PrevTotal;
            }

            // Monthly trend (last 12 months)
            for (int i = 11; i >= 0; i--)
            {
                var mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var mEnd   = mStart.AddMonths(1);
                var mAudits = _context.Auditorias.Where(a => a.Date >= mStart && a.Date < mEnd).ToList();
                var (my, mn, _) = CountChecklist(mAudits);
                int mPct = (my + mn) > 0 ? my * 100 / (my + mn) : 0;
                MonthlyTrend.Add(new MonthTrend
                {
                    Label = mStart.ToString("MMM yy"),
                    Pct   = mPct,
                    Count = mAudits.Count
                });
            }

            // Section map
            var sectionMap = new Dictionary<string, SectionStat>();
            foreach (var (_, _, key, name) in ChecklistItems)
                if (!sectionMap.ContainsKey(key))
                    sectionMap[key] = new SectionStat { name = name, key = key };

            // Item non-conformities
            var itemNos = new Dictionary<string, (string label, int yes, int no, int na)>();
            foreach (var (f, lbl, _, _) in ChecklistItems)
                itemNos[f] = (lbl, 0, 0, 0);

            foreach (var a in audits)
                foreach (var (f, _, key, _) in ChecklistItems)
                {
                    var val = typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "";
                    var (lbl, yes, no, na) = itemNos[f];
                    if (val == "YES")      { sectionMap[key].yes++; itemNos[f] = (lbl, yes + 1, no, na); }
                    else if (val == "NO")  { sectionMap[key].no++;  itemNos[f] = (lbl, yes, no + 1, na); }
                    else if (val == "N/A") { sectionMap[key].na++;  itemNos[f] = (lbl, yes, no, na + 1); }
                }

            TopNonConformities = itemNos
                .Where(x => (x.Value.yes + x.Value.no) > 0 && x.Value.no > 0)
                .Select(x => new ItemNonConformity
                {
                    label  = x.Value.label,
                    pctNo  = (x.Value.no + x.Value.yes) > 0 ? x.Value.no * 100 / (x.Value.no + x.Value.yes) : 0,
                    count  = x.Value.no
                })
                .OrderByDescending(x => x.count)
                .Take(10).ToList();

            // Razões de NO pré-definidas selecionadas (gráfico que substitui o Top NCs)
            ReasonStats = CountReasons(audits);

            foreach (var s in sectionMap.Values)
                s.pct = (s.yes + s.no) > 0 ? s.yes * 100 / (s.yes + s.no) : 0;

            SectionData  = sectionMap.Values.ToList();
            SectionNames = sectionMap.Keys.ToList();

            // Checklist field detail
            ChecklistDetail = itemNos.Select(kv => new ChecklistFieldDetail
            {
                Field      = kv.Key,
                Label      = kv.Value.label,
                SectionKey = ChecklistItems.FirstOrDefault(x => x.field == kv.Key).sectionKey ?? "",
                Yes        = kv.Value.yes,
                No         = kv.Value.no,
                Na         = kv.Value.na
            }).ToList();

            foreach (var g in audits.GroupBy(a => a.Airline).OrderByDescending(g => g.Count()))
            { AirlineLabels.Add(g.Key); AirlineValues.Add(g.Count()); }

            foreach (var g in audits.GroupBy(a => a.Agent).Where(g => !string.IsNullOrEmpty(g.Key)).OrderByDescending(g => g.Count()))
            { AgentLabels.Add(g.Key); AgentValues.Add(g.Count()); }

            foreach (var og in audits.GroupBy(a => a.AhmOfficer).Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key))
            {
                OfficerLabels.Add(og.Key);
                int oYes = 0, oNo = 0, oNA = 0;
                foreach (var a in og)
                    foreach (var (f, _, _, _) in ChecklistItems)
                    {
                        var val = typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "";
                        if (val == "YES") oYes++;
                        else if (val == "NO") oNo++;
                        else oNA++;
                    }
                int oPct = (oYes + oNo) > 0 ? oYes * 100 / (oYes + oNo) : 0;
                OfficerConformity.Add(oPct);
                OfficerStats[og.Key] = new OfficerStat { yes = oYes, no = oNo, na = oNA, pct = oPct };
                OfficerReasonDrill[og.Key] = CountReasons(og);
            }

            return Page();
        }
    }
}
