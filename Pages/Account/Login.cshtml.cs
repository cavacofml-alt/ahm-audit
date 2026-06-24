using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using System.Linq;

namespace AHM.Audit.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AuditDbContext _context;

        public LoginModel(AuditDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
            // Se já estiver autenticado, redirecionar para o dashboard
            if (HttpContext.Session.GetString("User") != null)
                Response.Redirect("/Index");
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Por favor preencha todos os campos.";
                return Page();
            }

            var user = _context.Users
                .FirstOrDefault(u => u.Username == Username && u.PasswordHash == Password);

            if (user == null)
            {
                ErrorMessage = "Utilizador ou password incorretos.";
                return Page();
            }

            HttpContext.Session.SetString("User", user.Username);
            return RedirectToPage("/Index");
        }
    }
}
