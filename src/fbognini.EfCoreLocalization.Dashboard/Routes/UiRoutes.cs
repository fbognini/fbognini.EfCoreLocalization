using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using System.Text;

namespace fbognini.EfCoreLocalization.Dashboard.Routes;

internal static class UiRoutes
{
    public static void MapRoutes(IEndpointRouteBuilder endpoints, string basePath)
    {
        endpoints.MapGet($"{basePath}", async context =>
        {
            context.Response.Redirect($"{basePath}/languages");
        });

        endpoints.MapGet($"{basePath}/languages", async context => await ServeIndexHtml(context, basePath));
        endpoints.MapGet($"{basePath}/texts", async context => await ServeIndexHtml(context, basePath));
        endpoints.MapGet($"{basePath}/translations", async context => await ServeIndexHtml(context, basePath));
    }

    private static async Task ServeIndexHtml(HttpContext context, string basePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "fbognini.EfCoreLocalization.Dashboard.wwwroot.index.html";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";

        using var reader = new StreamReader(stream);
        var html = await reader.ReadToEndAsync();

        html = html.Replace("{{BASE_PATH}}", basePath);

        var bytes = Encoding.UTF8.GetBytes(html);
        await context.Response.Body.WriteAsync(bytes);
    }
}
