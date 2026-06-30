using System;

namespace AHM.Audit.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsAdmin { get; set; } = false;
        public bool Active { get; set; } = true;

        // Vínculo direto a um Agent (Person)
        public int? PersonId { get; set; }

        // Permissões de dashboard (cada utilizador não-admin pode ter restrições)
        public bool CanViewDashboard { get; set; } = true;
        public bool CanViewSectionChart { get; set; } = true;
        public bool CanViewNonConformities { get; set; } = true;
        public bool CanViewGlobalConformity { get; set; } = true;
    }
}
