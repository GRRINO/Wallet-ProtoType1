using System.Net;
using System.Text.Json;

namespace Project1_WalletPJ.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
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
                _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);

                var status = ex switch
                {
                    KeyNotFoundException => HttpStatusCode.NotFound,
                    ArgumentException => HttpStatusCode.BadRequest,
                    InvalidOperationException => HttpStatusCode.BadRequest,
                    _ => HttpStatusCode.InternalServerError
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)status;

                var payload = JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    status = (int)status
                });

                await context.Response.WriteAsync(payload);
            }
        }
    }
}
