using fbognini.EfCoreLocalization.Dashboard.Routes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace fbognini.EfCoreLocalization.Dashboard.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the dashboard middleware and its endpoints. Call it after UseRouting.
    /// </summary>
    public static IApplicationBuilder UseEfCoreLocalizationDashboard(
        this IApplicationBuilder app,
        string path = DashboardConstants.DefaultPath,
        DashboardOptions? options = null)
    {
        var normalizedPath = NormalizePath(path);
        var dashboardOptions = options ?? new DashboardOptions();

        app.UseMiddleware<DashboardMiddleware>(dashboardOptions, normalizedPath);

        if (app is WebApplication webApp)
        {
            webApp.MapEfCoreLocalizationDashboard(normalizedPath);
            return app;
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapEfCoreLocalizationDashboard(normalizedPath);
        });


        return app;
    }

    /// <summary>
    /// Maps the dashboard endpoints. Call it after UseRouting and before UseEndpoints or MapControllers.
    /// </summary>
    public static IEndpointRouteBuilder MapEfCoreLocalizationDashboard(
        this IEndpointRouteBuilder endpoints,
        string path = DashboardConstants.DefaultPath)
    {
        var normalizedPath = NormalizePath(path);

        ApiRoutes.MapRoutes(endpoints, normalizedPath);
        StaticAssetRoutes.MapRoutes(endpoints, normalizedPath);
        UiRoutes.MapRoutes(endpoints, normalizedPath);

        return endpoints;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return DashboardConstants.DefaultPath;

        path = path.Trim();
        if (!path.StartsWith('/'))
            path = '/' + path;

        return path.TrimEnd('/');
    }
}
