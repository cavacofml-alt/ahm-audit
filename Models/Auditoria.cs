using System;

namespace AHM.Audit.Models
{
    public class Auditoria
    {
        public int Id { get; set; }

        // Estado
        public bool IsFinalized { get; set; } = false;
        public bool IsDraft { get; set; } = true;

        // Informação Geral
        public string Agent { get; set; } = "";
        public string AhmOfficer { get; set; } = "";
        public string Ticket { get; set; } = "";
        public string Airline { get; set; } = "";
        public string Aircraft { get; set; } = "";
        public string Registration { get; set; } = "";
        public DateTime Date { get; set; }
        public string RevisionUpdates { get; set; } = "";

        // Campos de correção
        public string CorrectionTicket { get; set; } = "";
        public string ReasonForRecertification { get; set; } = "";
        public string CorrectionsMade { get; set; } = "N/A";
        public string AircraftRecertified { get; set; } = "N/A";

        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Checklist (sem valor por defeito - vazio até o utilizador escolher)
        public string B1  { get; set; } = "";
        public string B2  { get; set; } = "";
        public string B3  { get; set; } = "";
        public string C1  { get; set; } = "";
        public string C2  { get; set; } = "";
        public string C2_3 { get; set; } = "";
        public string C3  { get; set; } = "";
        public string C4_TakeOff { get; set; } = "";
        public string C4_ZeroFuel { get; set; } = "";
        public string C4_Landing { get; set; } = "";
        public string C4_Inflight { get; set; } = "";
        public string C4_IdealTrim { get; set; } = "";
        public string C5  { get; set; } = "";
        public string C7_1 { get; set; } = "";
        public string D1  { get; set; } = "";
        public string D2  { get; set; } = "";
        public string D3  { get; set; } = "";
        public string D5_1 { get; set; } = "";
        public string D5_2 { get; set; } = "";
        public string D6_2 { get; set; } = "";
        public string E1_DOW { get; set; } = "";
        public string E1_MRW { get; set; } = "";
        public string E1_MTOW { get; set; } = "";
        public string E1_MZFW { get; set; } = "";
        public string E1_MLAW { get; set; } = "";
        public string E2_1 { get; set; } = "";
        public string E2_2 { get; set; } = "";
        public string E3_1 { get; set; } = "";
        public string G1  { get; set; } = "";
        public string RevisionUpdate { get; set; } = "";
        public string LIR { get; set; } = "";
        public string LS  { get; set; } = "";
        public string DatabasePrintout { get; set; } = "";

        // Razões para os NO (uma por item, separadas por ; no formato campo=razão)
        public string NoReasons { get; set; } = "";
    }
}
