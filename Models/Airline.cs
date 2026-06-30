using System;

namespace AHM.Audit.Models
{
    public class Airline
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";  // ex: TP, FR, EK
        public string Name { get; set; } = "";  // ex: TAP Air Portugal
        public bool Active { get; set; } = true;
    }
}
