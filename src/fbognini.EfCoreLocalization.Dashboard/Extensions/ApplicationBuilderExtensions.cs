using fbognini.EfCoreLocalization.Dashboard.Routes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace fbognini.EfCoreLocalization.Dashboard.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Localization Dashboard middleware and maps endpoints to the pipeline.
    /// This should be called after UseRouting().
    /// For WebApplication, use AddLocalizationDashboard() instead which handles both middleware and endpoints.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="path">The base path for the dashboard (default: /localization).</param>
    /// <param name="options">Optional dashboard configuration options.</param>
    /// <returns>The application builder for chaining.</returns>
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
    /// Maps the Localization Dashboard endpoints.
    /// This should be called after UseRouting() and before UseEndpoints() or MapControllers().
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="path">The base path for the dashboard (default: /localization).</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapEfCoreLocalizationDashboard(
        this IEndpointRouteBuilder endpoints,
        string path = DashboardConstants.DefaultPath)
    {
        var normalizedPath = NormalizePath(path);

        // Map API endpoints
        ApiRoutes.MapRoutes(endpoints, normalizedPath);
        
        // Map static assets
        StaticAssetRoutes.MapRoutes(endpoints, normalizedPath);
        
        // Map UI pages
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