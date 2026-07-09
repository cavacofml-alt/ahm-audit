using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using AHM.Audit.Data;

namespace AHM.Audit.Pages.Account
{
    // A política "login" já estava definida em Program.cs (10 pedidos/5min por IP)
    // mas nunca tinha sido aplicada a nenhum endpoint — este atributo é o que a ativa.
    [EnableRateLimiting("login")]
    public class LoginModel : PageModel
    {
        private readonly AuditDbContext _context;
        public LoginModel(AuditDbContext context) { _context = context; }

        [BindProperty] public string Username { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        // Após este número de tentativas falhadas seguidas, a conta fica bloqueada
        // temporariamente — proteção simples contra ataques de força bruta.
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("User") != null)
                return RedirectToPage("/Index");
            return Page();
        }

        public IActionResult OnPost()
        {
            // Guarda defensiva: um POST sem corpo/campos (ex.: pedido malformado, bot,
            // ou reenvio de formulário sem os campos preenchidos) pode chegar aqui com
            // Username/Password a null, o que fazia rebentar o BCrypt.Verify com uma
            // exceção não tratada em vez de mostrar simplesmente "credenciais inválidas".
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Preenche o utilizador e a password.";
                return Page();
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == Username && u.Active);
            if (user == null)
            {
                ErrorMessage = "Credenciais inválidas.";
                return Page();
            }

            // Conta bloqueada temporariamente por tentativas falhadas recentes
            if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.UtcNow)
            {
                var minutesLeft = Math.Ceiling((user.LockoutUntil.Value - DateTime.UtcNow).TotalMinutes);
                ErrorMessage = $"Conta temporariamente bloqueada por demasiadas tentativas falhadas. Tenta novamente daqui a {minutesLeft} minuto(s).";
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
                }
            }

            if (!passwordValid)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    user.FailedLoginAttempts = 0;
                    ErrorMessage = $"Conta bloqueada temporariamente por demasiadas tentativas falhadas. Tenta novamente daqui a {LockoutMinutes} minutos.";
                }
                else
                {
                    ErrorMessage = "Credenciais inválidas.";
                }
                _context.SaveChanges();
                return Page();
            }

            // Login bem-sucedido: repor contadores de bloqueio
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            _context.SaveChanges();

            HttpContext.Session.SetString("User", user.Username);
            return RedirectToPage("/Index");
        }
    }
}
