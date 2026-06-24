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

    public class IndexModel : PageModel
    {
        private readonly AuditDbContext _context;
        public IndexModel(AuditDbContext context) { _context = context; }

        public int Total    { get; set; }
        public int TotalYes { get; set; }
        public int TotalNo  { get; set; }
        public int TotalNA  { get; set; }
        public string PctYes { get; set; } = "0";
        public string PctNo  { get; set; } = "0";
        public string PctNA  { get; set; } = "0";

        public DateTime? FilterFrom { get; set; }
        public DateTime? FilterTo   { get; set; }
        public int CurrentQuarter   { get; set; }
        public int CurrentYear      { get; set; }

        public List<QuarterStat>  QuarterProgress  { get; set; } = new();
        public List<string>       AirlineLabels    { get; set; } = new();
        public List<int>          AirlineValues    { get; set; } = new();
        public List<string>       AgentLabels      { get; set; } = new();
        public List<int>          AgentValues      { get; set; } = new();
        public List<string>       OfficerLabels    { get; set; } = new();
        public List<int>          OfficerConformity { get; set; } = new();
        public List<string>       SectionNames     { get; set; } = new();
        public List<SectionStat>  SectionData      { get; set; } = new();
        public Dictionary<string, OfficerStat> OfficerStats { get; set; } = new();

        private static readonly (string field, string sectionKey, string sectionName)[] ChecklistItems = new[]
        {
            ("B1",            "B", "B - Standard/Crew/Pax Weights"),
            ("B2",            "B", "B - Standard/Crew/Pax Weights"),
            ("B3",            "B", "B - Standard/Crew/Pax Weights"),
            ("C1",            "C", "C - Balance & Fuel"),
            ("C2",            "C", "C - Balance & Fuel"),
            ("C2_3",          "C", "C - Balance & Fuel"),
            ("C3",            "C", "C - Balance & Fuel"),
            ("C4_TakeOff",    "C", "C - Balance & Fuel"),
            ("C4_ZeroFuel",   "C", "C - Balance & Fuel"),
            ("C4_Landing",    "C", "C - Balance & Fuel"),
            ("C4_Inflight",   "C", "C - Balance & Fuel"),
            ("C4_IdealTrim",  "C", "C - Balance & Fuel"),
            ("C5",            "C", "C - Balance & Fuel"),
            ("C7_1",          "C", "C - Balance & Fuel"),
            ("D1",            "D", "D - Dimensions & Cabin"),
            ("D2",            "D", "D - Dimensions & Cabin"),
            ("D3",            "D", "D - Dimensions & Cabin"),
            ("D5_1",          "D", "D - Dimensions & Cabin"),
            ("D5_2",          "D", "D - Dimensions & Cabin"),
            ("D6_2",          "D", "D - Dimensions & Cabin"),
            ("E1_DOW",        "E", "E - DOW/Crew/Pantry"),
            ("E1_MRW",        "E", "E - DOW/Crew/Pantry"),
            ("E1_MTOW",       "E", "E - DOW/Crew/Pantry"),
            ("E1_MZFW",       "E", "E - DOW/Crew/Pantry"),
            ("E1_MLAW",       "E", "E - DOW/Crew/Pantry"),
            ("E2_1",          "E", "E - DOW/Crew/Pantry"),
            ("E2_2",          "E", "E - DOW/Crew/Pantry"),
            ("E3_1",          "E", "E - DOW/Crew/Pantry"),
            ("G1",            "G", "G - ULD Compatibility"),
            ("RevisionUpdate","H", "H - Administrative"),
            ("LIR",           "H", "H - Administrative"),
            ("LS",            "H", "H - Administrative"),
            ("DatabasePrintout","H","H - Administrative"),
        };

        public IActionResult OnGet(DateTime? from, DateTime? to)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            var isAdmin = _context.Users.Any(u => u.Username == username && u.IsAdmin);
            if (!isAdmin) return RedirectToPage("/Auditorias/Index");
            ViewData["IsAdmin"] = true;

            FilterFrom = from;
            FilterTo   = to;

            var now = DateTime.Now;
            CurrentQuarter = (now.Month - 1) / 3 + 1;
            CurrentYear    = now.Year;

            // Quarter progress for current year
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

            var sectionMap = new Dictionary<string, SectionStat>();
            foreach (var (_, key, name) in ChecklistItems)
                if (!sectionMap.ContainsKey(key))
                    sectionMap[key] = new SectionStat { name = name, key = key };

            foreach (var a in audits)
                foreach (var (f, key, _) in ChecklistItems)
                {
                    var val = typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A";
                    if (val == "YES") { TotalYes++; sectionMap[key].yes++; }
                    else if (val == "NO") { TotalNo++; sectionMap[key].no++; }
                    else { TotalNA++; sectionMap[key].na++; }
                }

            foreach (var s in sectionMap.Values)
            {
                int t = s.yes + s.no + s.na;
                s.pct = t > 0 ? s.yes * 100 / t : 0;
            }

            SectionData  = sectionMap.Values.ToList();
            SectionNames = sectionMap.Keys.ToList();

            int grand = TotalYes + TotalNo + TotalNA;
            if (grand > 0)
            {
                PctYes = (TotalYes * 100 / grand).ToString();
                PctNo  = (TotalNo  * 100 / grand).ToString();
                PctNA  = (TotalNA  * 100 / grand).ToString();
            }

            foreach (var g in audits.GroupBy(a => a.Airline).OrderByDescending(g => g.Count()))
            { AirlineLabels.Add(g.Key); AirlineValues.Add(g.Count()); }

            foreach (var g in audits.GroupBy(a => a.Agent).Where(g => !string.IsNullOrEmpty(g.Key)).OrderByDescending(g => g.Count()))
            { AgentLabels.Add(g.Key); AgentValues.Add(g.Count()); }

            foreach (var og in audits.GroupBy(a => a.AhmOfficer).Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key))
            {
                OfficerLabels.Add(og.Key);
                int oYes = 0, oNo = 0, oNA = 0;
                foreach (var a in og)
                    foreach (var (f, _, _) in ChecklistItems)
                    {
                        var val = typeof(Auditoria).GetProperty(f)?.GetValue(a)?.ToString() ?? "N/A";
                        if (val == "YES") oYes++;
                        else if (val == "NO") oNo++;
                        else oNA++;
                    }
                int oTotal = oYes + oNo + oNA;
                int oPct   = oTotal > 0 ? oYes * 100 / oTotal : 0;
                OfficerConformity.Add(oPct);
                OfficerStats[og.Key] = new OfficerStat { yes = oYes, no = oNo, na = oNA, pct = oPct };
            }

            return Page();
        }
    }
}
