using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;

namespace AHM.Audit.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AuditDbContext _context;
        public LoginModel(AuditDbContext context) { _context = context; }

        [BindProperty] public string Username { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("User") != null)
                return RedirectToPage("/Index");
            return Page();
        }

        public IActionResult OnPost()
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == Username && u.Active);
            if (user == null)
            {
                ErrorMessage = "Credenciais inválidas.";
                return Page();
            }

            // Verificar password com BCrypt ou fallback para texto simples (migração gradual)
            bool passwordValid = false;
            if (user.PasswordHash.StartsWith("$2"))
            {
                // Password já tem hash BCrypt
                passwordValid = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);
            }
            else
            {
                // Password ainda em texto simples — verificar e migrar automaticamente
                if (user.PasswordHash == Password)
                {
                    passwordValid = true;
                    // Migrar para BCrypt automaticamente
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                    _context.SaveChanges();
                }
            }

            if (!passwordValid)
            {
                ErrorMessage = "Credenciais inválidas.";
                return Page();
            }

            HttpContext.Session.SetString("User", user.Username);
            return RedirectToPage("/Index");
        }
    }
}
