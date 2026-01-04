using fbognini.EfCoreLocalization.Dashboard.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace fbognini.EfCoreLocalization.Dashboard.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseEfCoreLocalizationDashboard(
            this WebApplication app,
            string pathMatch = "localization",
            DashboardOptions? options = null)
        {
            app.MapControllerRoute(
                name: DashboardContants.ApiRouteName,
                pattern: $"{pathMatch}/{{controller=Home}}/{{action=Index}}/{{id?}}",
                defaults: new { area = DashboardContants.Area, routeName = DashboardContants.ApiRouteName });

            app.MapControllerRoute(
                name: DashboardContants.ViewsRouteName,
                pattern: $"{pathMatch}/{{*view}}",
                defaults: new { controller = "Dispatcher", action = "Index", routeName = DashboardContants.ViewsRouteName });

            app.UseWhen(
                (context) => context.Request.RouteValues.TryGetValue("routeName", out var value) && value != null, 
                i18n => i18n.UseMiddleware<DashboardMiddleware>(options ?? new DashboardOptions()));

            return app;
        }
    }
}
