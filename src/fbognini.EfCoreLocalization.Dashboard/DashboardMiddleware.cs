using fbognini.EfCoreLocalization.Dashboard.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Dashboard
{
    internal class DashboardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DashboardOptions options;

        public DashboardMiddleware(RequestDelegate next, DashboardOptions dashboardOptions)
        {
            _next = next;
            this.options = dashboardOptions;
        }

        public async Task Invoke(HttpContext context)
        {
            var dashboardContext = new DashboardContext(context);

            foreach (var filter in options.Authorization)
            {
                if (!filter.Authorize(dashboardContext))
                {
                    context.Response.StatusCode = GetUnauthorizedStatusCode(context);
                    return;
                }
            }

            foreach (var filter in options.AsyncAuthorization)
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
}
