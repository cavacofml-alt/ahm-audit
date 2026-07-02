namespace AHM.Audit.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção não tratada em {Path}", context.Request.Path);
                context.Response.StatusCode = 500;
                if (!context.Response.HasStarted)
                {
                    context.Response.Redirect("/Error");
                }
            }
        }
    }
}
