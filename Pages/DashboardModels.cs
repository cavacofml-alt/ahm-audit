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
        public int count    { get; set; }
    }

    public class MonthTrend
    {
        public string Label { get; set; } = "";
        public int Pct      { get; set; }
        public int Count    { get; set; }
    }

    public class ChecklistFieldDetail
    {
        public string Field      { get; set; } = "";
        public string Label      { get; set; } = "";
        public string SectionKey { get; set; } = "";
        public int Yes { get; set; }
        public int No  { get; set; }
        public int Na  { get; set; }
    }

    // Razão de NO pré-definida e quantas vezes foi selecionada
    public class ReasonStat
    {
        public string Reason { get; set; } = "";
        public int Count     { get; set; }
    }
}
