using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using System.Text;

namespace fbognini.EfCoreLocalization.Dashboard.Routes;

internal static class UiRoutes
{
    public static void MapRoutes(IEndpointRouteBuilder endpoints, string basePath)
    {
        // Serve index.html for root and all UI routes (except API and assets)
        endpoints.MapGet($"{basePath}/{{*path}}", async context =>
        {
            var requestPath = context.Request.Path.Value ?? string.Empty;
            
            // Don't serve UI for API or assets paths
            if (requestPath.Contains(DashboardConstants.ApiPathPrefix) || 
                requestPath.Contains(DashboardConstants.AssetsPathPrefix))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "fbognini.EfCoreLocalization.Dashboard.wwwroot.index.html";

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            
            // Read and inject base path into HTML if needed
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();
            
            // Replace placeholder with actual base path for API calls
            html = html.Replace("{{BASE_PATH}}", basePath);
            
            var bytes = Encoding.UTF8.GetBytes(html);
            await context.Response.Body.WriteAsync(bytes);
        });
    }
}
