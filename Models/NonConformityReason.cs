using System;

namespace AHM.Audit.Models
{
    public class NonConformityReason
    {
        public int Id { get; set; }
        public string Reason { get; set; } = "";
        public bool Active { get; set; } = true;
    }
}
