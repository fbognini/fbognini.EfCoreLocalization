using fbognini.EfCoreLocalization.Dashboard.Authorization;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace fbognini.EfCoreLocalization.Dashboard;

internal class DashboardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DashboardOptions _options;
    private readonly string _pathMatch;

    public DashboardMiddleware(RequestDelegate next, DashboardOptions options, string pathMatch)
    {
        _next = next;
        _options = options;
        _pathMatch = pathMatch.TrimEnd('/');
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        if (!path.StartsWith(_pathMatch, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var dashboardContext = new DashboardContext(context);

        foreach (var filter in _options.Authorization)
        {
            if (!filter.Authorize(dashboardContext))
            {
                context.Response.StatusCode = GetUnauthorizedStatusCode(context);
                return;
            }
        }

        foreach (var filter in _options.AsyncAuthorization)
        {
            if (!await filter.AuthorizeAsync(dashboardContext))
            {
                context.Response.StatusCode = GetUnauthorizedStatusCode(context);
                return;
            }
        }

        await _next(context);
    }

    private static int GetUnauthorizedStatusCode(HttpContext httpContext)
    {
        return httpContext.User?.Identity?.IsAuthenticated == true
            ? (int)HttpStatusCode.Forbidden
            : (int)HttpStatusCode.Unauthorized;
    }
}