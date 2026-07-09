using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Auditorias
{
    public class CreateModel : PageModel
    {
        private readonly AuditDbContext _context;
        public CreateModel(AuditDbContext context) { _context = context; }

        [BindProperty]
        public Auditoria Auditoria { get; set; } = new();

        public List<string> Officers { get; set; } = new();
        public List<string> AirlineCodes { get; set; } = new();
        public List<string> NonConformityReasons { get; set; } = new();
        public List<string> RecentRegistrations { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string CurrentAgentName { get; set; } = "";
        public bool IsAdmin { get; set; }

        private static readonly string[] ChecklistFields = new[]
        {
            "B1","B2","B3","C1","C2","C2_3","C3","C4_TakeOff","C4_ZeroFuel",
            "C4_Landing","C4_Inflight","C4_IdealTrim","C5","C7_1","D1","D2",
            "D3","D5_1","D5_2","D6_2","E1_DOW","E1_MRW","E1_MTOW","E1_MZFW",
            "E1_MLAW","E2_1","E2_2","E3_1","G1","RevisionUpdate","LIR","LS","DatabasePrintout"
        };

        public IActionResult OnGet(int? draftId)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            IsAdmin = user?.IsAdmin ?? false;
            ViewData["IsAdmin"] = IsAdmin;

            // Agent vinculado ao utilizador
            if (user?.PersonId != null)
            {
                var person = _context.Persons.Find(user.PersonId);
                CurrentAgentName = person?.Name ?? "";
            }

            if (string.IsNullOrEmpty(CurrentAgentName))
            {
                ErrorMessage = "O teu utilizador não está associado a nenhum Agent. Contacta um administrador.";
                LoadDropdowns();
                return Page();
            }

            // Recuperar rascunho específico (ex.: link "Continuar" na lista de auditorias)
            // ou criar sempre uma nova. Um agent pode ter várias auditorias por finalizar
            // em simultâneo — "Nova Auditoria" nunca deve ficar "preso" a reabrir
            // automaticamente a última que ficou incompleta.
            Auditoria? draft = null;
            if (draftId.HasValue)
            {
                draft = _context.Auditorias.Find(draftId.Value);
            }

            if (draft != null)
            {
                Auditoria = draft;
            }
            else
            {
                Auditoria.Agent = CurrentAgentName;
                Auditoria.Date = DateTime.Today;
            }

            LoadDropdowns();
            LoadRecentRegistrations();
            return Page();
        }

        // Permite ao utilizador descartar o rascunho autosave atual e começar
        // uma auditoria completamente nova, em vez de ficar preso a reabrir
        // sempre o mesmo rascunho por acabar.
        public IActionResult OnGetDiscard(int id)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            var isAdmin = user?.IsAdmin ?? false;
            var agentName = user?.PersonId != null ? _context.Persons.Find(user.PersonId)?.Name : null;

            var draft = _context.Auditorias.Find(id);
            // Só permite descartar rascunhos (nunca auditorias finalizadas) e só o
            // próprio agent (ou um admin) pode descartar o seu rascunho.
            if (draft != null && draft.IsDraft && (isAdmin || draft.Agent == agentName))
            {
                _context.Auditorias.Remove(draft);
                _context.SaveChanges();
            }

            return RedirectToPage("Create");
        }

        public IActionResult OnPost(string? action)
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return RedirectToPage("/Account/Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            IsAdmin = user?.IsAdmin ?? false;
            ViewData["IsAdmin"] = IsAdmin;

            if (user?.PersonId != null)
            {
                var person = _context.Persons.Find(user.PersonId);
                CurrentAgentName = person?.Name ?? "";
            }
            // Rede de segurança: nunca deixar Agent em branco silenciosamente.
            if (string.IsNullOrWhiteSpace(CurrentAgentName)) CurrentAgentName = username ?? "";

            LoadDropdowns();
            LoadRecentRegistrations();

            Auditoria existing;
            if (Auditoria.Id == 0)
            {
                // O utilizador preencheu a Informação Geral mas nunca chegou a marcar
                // nenhum item da checklist — nesse caso o autosave nunca criou a linha
                // na base de dados. Em vez de falhar com "não encontrada", cria-se agora
                // a auditoria (a validação da checklist mais abaixo vai avisar de forma
                // clara que faltam itens por preencher).
                existing = new Auditoria { CreatedAt = DateTime.UtcNow, IsDraft = true, Date = DateTime.Today, Agent = CurrentAgentName };
                _context.Auditorias.Add(existing);
                _context.SaveChanges();
            }
            else
            {
                var found = _context.Auditorias.Find(Auditoria.Id);
                if (found == null)
                {
                    ErrorMessage = "Auditoria não encontrada. Tenta novamente.";
                    return Page();
                }
                existing = found;
            }

            // Ticket obrigatório e único
            if (string.IsNullOrWhiteSpace(Auditoria.Ticket))
            {
                ErrorMessage = "O Ticket Number é obrigatório.";
                Auditoria = existing;
                return Page();
            }
            if (!Auditoria.Ticket.StartsWith("#"))
                Auditoria.Ticket = "#" + Auditoria.Ticket;

            if (_context.Auditorias.Any(a => a.Ticket == Auditoria.Ticket && a.Id != existing.Id))
            {
                ErrorMessage = $"O Ticket '{Auditoria.Ticket}' já existe.";
                Auditoria = existing;
                return Page();
            }

            // Validar que todos os 33 itens da checklist estão preenchidos
            var missingItems = new List<string>();
            foreach (var field in ChecklistFields)
            {
                var val = typeof(Auditoria).GetProperty(field)?.GetValue(existing)?.ToString();
                if (string.IsNullOrEmpty(val))
                    missingItems.Add(field);
            }
            if (missingItems.Any())
            {
                ErrorMessage = $"Faltam preencher {missingItems.Count} item(ns) da checklist: {string.Join(", ", missingItems.Take(5))}{(missingItems.Count > 5 ? "..." : "")}";
                Auditoria = existing;
                return Page();
            }

            // Validar que todos os NO têm razão
            var noFields = ChecklistFields.Where(f => typeof(Auditoria).GetProperty(f)?.GetValue(existing)?.ToString() == "NO").ToList();
            var existingReasons = string.IsNullOrEmpty(existing.NoReasons)
                ? new Dictionary<string, string>()
                : existing.NoReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split('=')).Where(p => p.Length == 2).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last()[1]);

            var missingReasons = noFields.Where(f => !existingReasons.ContainsKey(f) || string.IsNullOrEmpty(existingReasons[f])).ToList();
            if (missingReasons.Any())
            {
                ErrorMessage = $"Faltam razões para {missingReasons.Count} item(ns) marcado(s) como NO.";
                Auditoria = existing;
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Auditoria.CorrectionTicket))
            {
                if (!Auditoria.CorrectionTicket.StartsWith("#"))
                    Auditoria.CorrectionTicket = "#" + Auditoria.CorrectionTicket;
                if (_context.Auditorias.Any(a => a.CorrectionTicket == Auditoria.CorrectionTicket && a.Id != existing.Id))
                {
                    ErrorMessage = $"O Correction Ticket '{Auditoria.CorrectionTicket}' já existe.";
                    Auditoria = existing;
                    return Page();
                }
            }

            if (Auditoria.AircraftRecertified == "YES" && string.IsNullOrWhiteSpace(Auditoria.CorrectionTicket))
            {
                ErrorMessage = "O Correction Ticket é obrigatório quando o Aircraft é Re-certificado.";
                Auditoria = existing;
                return Page();
            }

            if (!string.IsNullOrEmpty(CurrentAgentName) && CurrentAgentName == Auditoria.AhmOfficer)
            {
                ErrorMessage = "O Audit Agent não pode ser o mesmo que o AHM Officer.";
                Auditoria = existing;
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Auditoria.Airline))
                Auditoria.Airline = Auditoria.Airline.ToUpper();

            // Atualizar campos no existing (que já tem os valores da checklist via autosave)
            existing.Ticket = Auditoria.Ticket;
            existing.AhmOfficer = Auditoria.AhmOfficer ?? "";
            existing.Airline = Auditoria.Airline ?? "";
            existing.Aircraft = Auditoria.Aircraft ?? "";
            existing.Registration = Auditoria.Registration ?? "";
            existing.RevisionUpdates = Auditoria.RevisionUpdates ?? "";
            existing.CorrectionTicket = Auditoria.CorrectionTicket ?? "";
            existing.CorrectionsMade = Auditoria.CorrectionsMade ?? "N/A";
            existing.AircraftRecertified = Auditoria.AircraftRecertified ?? "N/A";
            existing.ReasonForRecertification = Auditoria.ReasonForRecertification ?? "";
            existing.Notes = Auditoria.Notes ?? "";
            existing.Date = Auditoria.Date == default ? DateTime.Today : Auditoria.Date;
            existing.IsDraft = false;
            existing.IsFinalized = action == "finalize";

            _context.SaveChanges();
            return RedirectToPage("Index");
        }

        private void LoadDropdowns()
        {
            Officers = _context.Persons.Where(p => p.Role == "Officer" && p.Active && p.Name != CurrentAgentName).Select(p => p.Name).OrderBy(n => n).ToList();
            AirlineCodes = _context.Airlines.Where(a => a.Active).Select(a => a.Code).OrderBy(c => c).ToList();
            NonConformityReasons = _context.NonConformityReasons.Where(r => r.Active).Select(r => r.Reason).ToList();
        }

        private void LoadRecentRegistrations()
        {
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            RecentRegistrations = _context.Auditorias
                .Where(a => a.Date >= sixMonthsAgo && !a.IsDraft
                    && !string.IsNullOrEmpty(a.Registration)
                    && !string.IsNullOrEmpty(a.Airline)
                    && !string.IsNullOrEmpty(a.Aircraft))
                .Select(a => a.Airline + "|" + a.Aircraft + "|" + a.Registration)
                .Distinct().ToList();
        }
    }
}
