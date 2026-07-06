using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        private readonly AuditDbContext _context;
        public SettingsModel(AuditDbContext context) { _context = context; }

        public List<Airline> Airlines { get; set; } = new();
        public List<NonConformityReason> Reasons { get; set; } = new();
        public string Message { get; set; } = "";
        public bool IsError { get; set; }

        public IActionResult OnGet()
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            LoadLists();
            return Page();
        }

        public IActionResult OnPostAddAirline(string code, string name)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;

            code = (code ?? "").Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(code) || code.Length > 3)
            {
                Message = "Código de airline inválido (máx. 3 caracteres)."; IsError = true;
                LoadLists(); return Page();
            }
            if (_context.Airlines.Any(a => a.Code == code))
            {
                Message = $"O código '{code}' já existe."; IsError = true;
                LoadLists(); return Page();
            }
            _context.Airlines.Add(new Airline { Code = code, Name = name ?? "", Active = true });
            _context.SaveChanges();
            Message = $"Airline '{code}' adicionada.";
            LoadLists();
            return Page();
        }

        public IActionResult OnPostToggleAirline(int id)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var a = _context.Airlines.Find(id);
            if (a != null) { a.Active = !a.Active; _context.SaveChanges(); }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostAddReason(string reason)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;

            if (string.IsNullOrWhiteSpace(reason))
            {
                Message = "O motivo não pode estar vazio."; IsError = true;
                LoadLists(); return Page();
            }
            if (_context.NonConformityReasons.Any(r => r.Reason.ToLower() == reason.ToLower()))
            {
                Message = "Este motivo já existe."; IsError = true;
                LoadLists(); return Page();
            }
            _context.NonConformityReasons.Add(new NonConformityReason { Reason = reason, Active = true });
            _context.SaveChanges();
            Message = "Motivo adicionado.";
            LoadLists();
            return Page();
        }

        public IActionResult OnPostToggleReason(int id)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var r = _context.NonConformityReasons.Find(id);
            if (r != null) { r.Active = !r.Active; _context.SaveChanges(); }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostDeleteReason(int id)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var r = _context.NonConformityReasons.Find(id);
            if (r != null) { _context.NonConformityReasons.Remove(r); _context.SaveChanges(); }
            LoadLists();
            return Page();
        }

        private void LoadLists()
        {
            Airlines = _context.Airlines.OrderBy(a => a.Code).ToList();
            Reasons = _context.NonConformityReasons.OrderBy(r => r.Reason).ToList();
        }

        private bool IsAdmin()
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return false;
            return _context.Users.Any(u => u.Username == username && u.IsAdmin);
        }
    }
}
