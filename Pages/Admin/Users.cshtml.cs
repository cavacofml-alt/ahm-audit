using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Admin
{
    [IgnoreAntiforgeryToken]
    public class UsersModel : PageModel
    {
        private readonly AuditDbContext _context;
        public UsersModel(AuditDbContext context) { _context = context; }

        public List<User> Users { get; set; } = new();
        public List<Person> Agents { get; set; } = new();
        public string Message { get; set; } = "";
        public bool IsError { get; set; }

        public IActionResult OnGet()
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            LoadLists();
            return Page();
        }

        public IActionResult OnPostCreate(string newUsername, string newPassword, bool isAdmin, int? personId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;

            if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newPassword))
            {
                Message = "Username e password são obrigatórios."; IsError = true;
                LoadLists(); return Page();
            }

            if (_context.Users.Any(u => u.Username.ToLower() == newUsername.ToLower()))
            {
                Message = $"O username '{newUsername}' já existe."; IsError = true;
                LoadLists(); return Page();
            }

            _context.Users.Add(new User
            {
                Username = newUsername, PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword), IsAdmin = isAdmin, Active = true,
                PersonId = personId,
                CanViewDashboard = true, CanViewSectionChart = true,
                CanViewNonConformities = true, CanViewGlobalConformity = true
            });
            _context.SaveChanges();
            Message = $"Utilizador '{newUsername}' criado com sucesso.";
            LoadLists();
            return Page();
        }

        public IActionResult OnPostResetPassword(int userId, string newPass)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPass);
                _context.SaveChanges();
                Message = $"Password de '{user.Username}' atualizada.";
            }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostEditUsername(int userId, string newUsername)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;

            if (_context.Users.Any(u => u.Username.ToLower() == newUsername.ToLower() && u.Id != userId))
            {
                Message = $"O username '{newUsername}' já existe."; IsError = true;
                LoadLists(); return Page();
            }

            var user = _context.Users.Find(userId);
            if (user != null) { user.Username = newUsername; _context.SaveChanges(); Message = $"Username atualizado para '{newUsername}'."; }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostSetPerson(int userId, int? personId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.PersonId = personId;
                _context.SaveChanges();
                var person = personId.HasValue ? _context.Persons.Find(personId.Value) : null;
                Message = person != null ? $"'{user.Username}' associado a '{person.Name}'." : $"Associação removida de '{user.Username}'.";
            }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostSetPermissions(int userId,
            bool canViewDashboard, bool canViewSectionChart, bool canViewNonConformities,
            bool canViewGlobalConformity, bool canViewTrend, bool canViewHeatmap,
            bool canViewAirlineChart, bool canViewAgentChart, bool canViewOfficerChart,
            bool canViewComparativeChart, bool canViewQuarterProgress)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.CanViewDashboard        = canViewDashboard;
                user.CanViewSectionChart     = canViewSectionChart;
                user.CanViewNonConformities  = canViewNonConformities;
                user.CanViewGlobalConformity = canViewGlobalConformity;
                user.CanViewTrend            = canViewTrend;
                user.CanViewHeatmap          = canViewHeatmap;
                user.CanViewAirlineChart     = canViewAirlineChart;
                user.CanViewAgentChart       = canViewAgentChart;
                user.CanViewOfficerChart     = canViewOfficerChart;
                user.CanViewComparativeChart = canViewComparativeChart;
                user.CanViewQuarterProgress  = canViewQuarterProgress;
                _context.SaveChanges();
                Message = $"Permissões de '{user.Username}' atualizadas.";
            }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostToggleActive(int userId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var user = _context.Users.Find(userId);
            if (user != null) { user.Active = !user.Active; _context.SaveChanges(); Message = $"Conta '{user.Username}' {(user.Active ? "ativada" : "desativada")}."; }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostToggleAdmin(int userId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var currentUser = HttpContext.Session.GetString("User");
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                if (user.Username == currentUser && user.IsAdmin)
                {
                    Message = "Não podes remover o teu próprio acesso de admin."; IsError = true;
                    LoadLists(); return Page();
                }
                user.IsAdmin = !user.IsAdmin;
                _context.SaveChanges();
                Message = $"'{user.Username}' {(user.IsAdmin ? "agora é admin" : "deixou de ser admin")}.";
            }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostDelete(int userId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var currentUser = HttpContext.Session.GetString("User");
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                if (user.Username == currentUser)
                {
                    Message = "Não podes apagar a tua própria conta."; IsError = true;
                    LoadLists(); return Page();
                }
                _context.Users.Remove(user);
                _context.SaveChanges();
                Message = $"Utilizador '{user.Username}' apagado.";
            }
            LoadLists();
            return Page();
        }

        private void LoadLists()
        {
            Users = _context.Users.OrderBy(u => u.Username).ToList();
            Agents = _context.Persons.Where(p => p.Role == "Agent" && p.Active).OrderBy(p => p.Name).ToList();
        }

        private bool IsAdmin()
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return false;
            return _context.Users.Any(u => u.Username == username && u.IsAdmin);
        }
    }
}
