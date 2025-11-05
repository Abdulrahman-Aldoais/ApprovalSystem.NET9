using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalSystem.API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var tenantId = context.Request.Headers["X-Tenant-ID"].ToString();
            
            if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsedTenantId))
            {
                context.Items["CurrentTenantId"] = parsedTenantId;
            }

            await _next(context);
        }
    }
}
