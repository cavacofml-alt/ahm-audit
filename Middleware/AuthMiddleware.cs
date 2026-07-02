using AHM.Audit.Data;

namespace AHM.Audit.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        // Páginas públicas que não precisam de autenticação
        private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Account/Login",
            "/Account/Logout",
            "/api/autosave",
            "/api/autosave-field"
        };

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Permitir ficheiros estáticos e caminhos públicos
            if (path.StartsWith("/css") || path.StartsWith("/js") ||
                path.StartsWith("/lib") || path.StartsWith("/images") ||
                path.StartsWith("/favicon"))
            {
                await _next(context);
                return;
            }

            // Verificar se é uma rota pública
            if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Verificar sessão
            var username = context.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                context.Response.Redirect("/Account/Login");
                return;
            }

            // Verificar se o utilizador ainda existe e está ativo
            var db = context.RequestServices.GetRequiredService<AuditDbContext>();
            var user = db.Users.FirstOrDefault(u => u.Username == username && u.Active);
            if (user == null)
            {
                context.Session.Clear();
                context.Response.Redirect("/Account/Login");
                return;
            }

            await _next(context);
        }
    }
}
