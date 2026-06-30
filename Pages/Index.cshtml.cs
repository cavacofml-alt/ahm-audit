using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace AHM.Audit.Pages
{
    public class SectionStat
    {
        public string name { get; set; } = "";
        public string key  { get; set; } = "";
        public int yes { get; set; }
        public int no  { get; set; }
        public int na  { get; set; }
        public int pct { get; set; }
    }

    public class OfficerStat
    {
        public int yes { get; set; }
        public int no  { get; set; }
        public int na  { get; set; }
        public int pct { get; set; }
    }

    public class QuarterStat
    {
        public string Label { get; set; } = "";
        public int Count    { get; set; }
    }

    public class ItemNonConformity
    {
        public string label { get; set; } = "";
        public int pctNo    { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly AuditDbContext _context;
        public IndexModel(AuditDbContext context) { _context = context; }

        public bool IsAdmin { get; set; }
        public bool CanViewSectionChart { get; set; } = true;
        public bool CanViewNonConformities { get; set; } = true;
        public bool CanViewGlobalConformity { get; set; } = true;
        public int Total    { get; set; }
        public int TotalYes { get; set; }
        public int TotalNo  { get; set; }
        public int TotalNA  { get; set; }
        public string PctYes { get; set; } = "0";
        public string PctNo  { get; set; } = "0";
        public string PctNA  { get; set; } = "0";

        public int PrevTotal     { get; set; }
        public string PrevPctYes { get; set; } = "0";
        public int DiffTotal     { get; set; }
        public int DiffPctYes    { get; set; }
        public int CompareYear   { get; set; }
        public int CurrentDataYear { get; set; }

        public DateTime? FilterFrom { get; set; }
        public DateTime? FilterTo   { get; set; }
        public int CurrentQuarter   { get; set; }
        public int CurrentYear      { get; set; }

        public List<QuarterStat>       QuarterProgress      { get; set; } = new();
        public List<string>            AirlineLabels        { get; set; } = new();
        public List<int>               AirlineValues        { get; set; } = new();
        public List<string>            AgentLabels          { get; set; } = new();
        public List<int>               AgentValues          { get; set; } = new();
        public List<string>            OfficerLabels        { get; set; } = new();
        public List<int>               OfficerConformity    { get; set; } = new();
        public List<string>            SectionNames         { get; set; } = new();
        public List<SectionStat>       SectionData          { get; set; } = new();
        public List<ItemNonConformity> TopNonConformities   { get; set; } = new();
        public Dictionary<string, OfficerStat> OfficerStats { get; set; } = new();

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
                    var val = typeof(Auditoria).GetProperty(f)?.GetValue(audit)?.ToString() ?? "N/A";
                    if (val == "YES") y++;
                    else if (val == "NO") n++;
                    else a++;
                }
            return (y, n, a);
        }

        public IActionResult OnGet(DateTime? from, DateTime? to)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToPage("/Account/Login");

            IsAdmin = user.IsAdmin;
            ViewData["IsAdmin"] = IsAdmin;

            // Não-admins precisam de permissão para ver o dashboard
            if (!IsAdmin && !user.CanViewDashboard)
                return RedirectToPage("/Auditorias/Index");

            CanViewSectionChart = IsAdmin || user.CanViewSectionChart;
            CanViewNonConformities = IsAdmin || user.CanViewNonConformities;
            CanViewGlobalConformity = IsAdmin || user.CanViewGlobalConformity;

            FilterFrom = from;
            FilterTo   = to;

            var now = DateTime.Now;
            CurrentQuarter = (now.Month - 1) / 3 + 1;
            CurrentYear    = now.Year;

            for (int q = 1; q <= 4; q++)
            {
                var qStart = new DateTime(now.Year, (q - 1) * 3 + 1, 1);
                var qEnd   = qStart.AddMonths(3);
                var count  = _context.Auditorias.Count(a => a.Date >= qStart && a.Date < qEnd);
                QuarterProgress.Add(new QuarterStat { Label = $"Q{q} {now.Year}", Count = count });
            }

            var query = _context.Auditorias.AsQueryable();
            if (from.HasValue) query = query.Where(a => a.Date >= from.Value);
            if (to.HasValue)   query = query.Where(a => a.Date <= to.Value);
            var audits = query.ToList();

            Total = audits.Count;
            var (cy, cn, ca) = CountChecklist(audits);
            TotalYes = cy; TotalNo = cn; TotalNA = ca;

            int grand = TotalYes + TotalNo;
            if (grand > 0)
            {
                PctYes = (TotalYes * 100 / grand).ToString();
                PctNo  = (TotalNo  * 100 / grand).ToString();
                PctNA  = (TotalNA  * 100 / grand).ToString();
            }

            // Comparação ano anterior
            var dataYear = from.HasValue ? from.Value.Year : now.Year;
            CurrentDataYear = dataYear;
            CompareYear = dataYear - 1;

            var prevAudits = _context.AuditoriaArchives.Where(a => a.ArchiveYear == CompareYear).ToList();
            PrevTotal = prevAudits.Count;

            if (prevAudits.Any())
            {
                int py = 0, pn = 0, pa = 0;
                foreach (var a in prevAudits)
                    foreach (var (f, _, _, _) in ChecklistItems)
                    {
                        var val = typeof(AuditoriaArchive).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A";
                        if (val == "YES") py++;
                        else if (val == "NO") pn++;
                        else pa++;
                    }
                int prevPct = (py + pn) > 0 ? py * 100 / (py + pn) : 0;
                PrevPctYes = prevPct.ToString();
                DiffPctYes = int.Parse(PctYes) - prevPct;
                DiffTotal  = Total - PrevTotal;
            }

            // Section map
            var sectionMap = new Dictionary<string, SectionStat>();
            foreach (var (_, _, key, name) in ChecklistItems)
                if (!sectionMap.ContainsKey(key))
                    sectionMap[key] = new SectionStat { name = name, key = key };

            // Top não conformidades por item
            var itemNos = new Dictionary<string, (string label, int yes, int no, int na)>();
            foreach (var (f, lbl, _, _) in ChecklistItems)
                itemNos[f] = (lbl, 0, 0, 0);

            foreach (var a in audits)
                foreach (var (f, _, key, _) in ChecklistItems)
                {
                    var val = typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A";
                    var (lbl, yes, no, na) = itemNos[f];
                    if (val == "YES") { sectionMap[key].yes++; itemNos[f] = (lbl, yes + 1, no, na); }
                    else if (val == "NO") { sectionMap[key].no++; itemNos[f] = (lbl, yes, no + 1, na); }
                    else { sectionMap[key].na++; itemNos[f] = (lbl, yes, no, na + 1); }
                }

            TopNonConformities = itemNos
                .Where(x => (x.Value.yes + x.Value.no) > 0 && x.Value.no > 0)
                .Select(x => new ItemNonConformity
                {
                    label  = x.Value.label,
                    pctNo  = (x.Value.no + x.Value.yes) > 0 ? x.Value.no * 100 / (x.Value.no + x.Value.yes) : 0
                })
                .OrderByDescending(x => x.pctNo)
                .Take(10)
                .ToList();

            foreach (var s in sectionMap.Values)
            {
                s.pct = (s.yes + s.no) > 0 ? s.yes * 100 / (s.yes + s.no) : 0;
            }

            SectionData  = sectionMap.Values.ToList();
            SectionNames = sectionMap.Keys.ToList();

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
                        var val = typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A";
                        if (val == "YES") oYes++;
                        else if (val == "NO") oNo++;
                        else oNA++;
                    }
                int oPct = (oYes + oNo) > 0 ? oYes * 100 / (oYes + oNo) : 0;
                OfficerConformity.Add(oPct);
                OfficerStats[og.Key] = new OfficerStat { yes = oYes, no = oNo, na = oNA, pct = oPct };
            }

            return Page();
        }
    }
}
