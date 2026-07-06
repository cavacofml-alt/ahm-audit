namespace AHM.Audit.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsAdmin { get; set; } = false;
        public bool Active { get; set; } = true;
        public int? PersonId { get; set; }

        // Bloqueio de conta após várias tentativas de login falhadas
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil  { get; set; }

        // Permissões de dashboard
        public bool CanViewDashboard          { get; set; } = true;
        public bool CanViewNonConformities    { get; set; } = true;
        public bool CanViewGlobalConformity   { get; set; } = true;
        public bool CanViewTrend              { get; set; } = true;
        public bool CanViewHeatmap            { get; set; } = true;
        public bool CanViewAirlineChart       { get; set; } = true;
        public bool CanViewAgentChart         { get; set; } = true;
        public bool CanViewOfficerChart       { get; set; } = true;
        public bool CanViewComparativeChart   { get; set; } = true;
        public bool CanViewQuarterProgress    { get; set; } = true;
    }
}
