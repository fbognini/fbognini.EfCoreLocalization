using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Reflection;

namespace fbognini.EfCoreLocalization.Dashboard.Routes;

internal static class StaticAssetRoutes
{
    public static void MapRoutes(IEndpointRouteBuilder endpoints, string basePath)
    {
        var assetsPath = $"{basePath}{DashboardConstants.AssetsPathPrefix}";

        endpoints.MapGet($"{assetsPath}/{{*path}}", async context =>
        {
            var path = context.Request.RouteValues["path"]?.ToString() ?? string.Empty;
            var assembly = Assembly.GetExecutingAssembly();

            // MSBuild names an embedded resource after the namespace plus its relative path with dots instead of slashes, so css/site.css becomes fbognini.EfCoreLocalization.Dashboard.wwwroot.css.site.css.
            var normalizedPath = path.Replace('/', '.').Replace('\\', '.');
            var resourceName = $"fbognini.EfCoreLocalization.Dashboard.wwwroot.{normalizedPath}";

            await using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            context.Response.ContentType = extension switch
            {
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                ".eot" => "application/vnd.ms-fontobject",
                _ => "application/octet-stream"
            };

            await stream.CopyToAsync(context.Response.Body);
        });
    }
}
