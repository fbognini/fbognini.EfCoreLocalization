using fbognini.EfCoreLocalization.Dashboard.Handlers.Languages;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Texts;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Translations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace fbognini.EfCoreLocalization.Dashboard.Routes;

internal static class ApiRoutes
{
    public static void MapRoutes(IEndpointRouteBuilder endpoints, string basePath)
    {
        var apiPath = $"{basePath}{DashboardConstants.ApiPathPrefix}";

        // Languages endpoints
        endpoints.MapGet($"{apiPath}/languages", LanguageHandlers.GetPaginatedLanguages);
        endpoints.MapPost($"{apiPath}/languages", LanguageHandlers.CreateLanguage);
        endpoints.MapPut($"{apiPath}/languages/{{id}}", LanguageHandlers.UpdateLanguage);

        // Texts endpoints
        endpoints.MapGet($"{apiPath}/texts", TextHandlers.GetPaginatedTexts);
        endpoints.MapPost($"{apiPath}/texts", TextHandlers.CreateText);
        endpoints.MapDelete($"{apiPath}/texts/{{textId}}/{{resourceId}}", TextHandlers.DeleteText);

        // Translations endpoints
        endpoints.MapGet($"{apiPath}/translations", TranslationHandlers.GetPaginatedTranslations);
        endpoints.MapPut($"{apiPath}/translations", TranslationHandlers.UpdateTranslation);
    }
}
