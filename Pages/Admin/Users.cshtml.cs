using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly AuditDbContext _context;
        public UsersModel(AuditDbContext context) { _context = context; }

        public List<User> Users { get; set; } = new();
        public string Message { get; set; } = "";

        public IActionResult OnGet()
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            Users = _context.Users.ToList();
            return Page();
        }

        public IActionResult OnPostCreate(string newUsername, string newPassword, bool isAdmin)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            _context.Users.Add(new User { Username = newUsername, PasswordHash = newPassword, IsAdmin = isAdmin, Active = true });
            _context.SaveChanges();
            Message = $"Utilizador '{newUsername}' criado com sucesso.";
            Users = _context.Users.ToList();
            return Page();
        }

        public IActionResult OnPostResetPassword(int userId, string newPass)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var user = _context.Users.Find(userId);
            if (user != null) { user.PasswordHash = newPass; _context.SaveChanges(); Message = $"Password de '{user.Username}' atualizada."; }
            Users = _context.Users.ToList();
            return Page();
        }

        public IActionResult OnPostEditUsername(int userId, string newUsername)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var user = _context.Users.Find(userId);
            if (user != null) { user.Username = newUsername; _context.SaveChanges(); Message = $"Username atualizado para '{newUsername}'."; }
            Users = _context.Users.ToList();
            return Page();
        }

        public IActionResult OnPostToggleActive(int userId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var user = _context.Users.Find(userId);
            if (user != null) { user.Active = !user.Active; _context.SaveChanges(); Message = $"Conta '{user.Username}' {(user.Active ? "ativada" : "desativada")}."; }
            Users = _context.Users.ToList();
            return Page();
        }

        private bool IsAdmin()
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return false;
            return _context.Users.Any(u => u.Username == username && u.IsAdmin);
        }
    }
}
