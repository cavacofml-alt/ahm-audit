namespace AHM.Audit.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = ""; // "Agent" or "Officer"
        public bool Active { get; set; } = true;
    }
}
