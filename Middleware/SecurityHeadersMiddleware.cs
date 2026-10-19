namespace AHM.Audit.Middleware
{
    /// <summary>
    /// Acrescenta cabeçalhos de segurança a todas as respostas (incluindo ficheiros estáticos
    /// e páginas de erro, por correr antes de tudo o resto no pipeline). O Bootstrap, os ícones
    /// do Bootstrap e o Chart.js vêm de cdn.jsdelivr.net (não estão em wwwroot/lib) — a CSP
    /// tem de permitir esse domínio para a app continuar a funcionar. As páginas têm scripts e
    /// estilos inline (não há um passo de build que gere nonces), por isso 'unsafe-inline' fica
    /// em script-src/style-src — mitigado por não haver, hoje, nenhum ponto de entrada de HTML
    /// de terceiros nas páginas (texto de utilizadores é sempre mostrado via Razor, que escapa
    /// por omissão).
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        private const string Csp =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "font-src 'self' https://cdn.jsdelivr.net; " +
            "img-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Content-Security-Policy"] = Csp;
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
