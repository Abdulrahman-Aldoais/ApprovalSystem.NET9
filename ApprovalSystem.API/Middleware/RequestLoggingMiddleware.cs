using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApprovalSystem.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.LogInformation("طلب جديد: {Method} {Path} {QueryString}", 
                context.Request.Method, 
                context.Request.Path, 
                context.Request.QueryString);

            await _next(context);

            stopwatch.Stop();
            
            _logger.LogInformation("تم إنجاز الطلب: {Method} {Path} - الحالة {StatusCode} - الوقت {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
