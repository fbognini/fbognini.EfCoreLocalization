using fbognini.EfCoreLocalization.Dashboard.Handlers.Languages;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Portability;
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

        endpoints.MapGet($"{apiPath}/languages", LanguageHandlers.GetPaginatedLanguages);
        endpoints.MapPost($"{apiPath}/languages", LanguageHandlers.CreateLanguage);
        endpoints.MapPut($"{apiPath}/languages/{{id}}", LanguageHandlers.UpdateLanguage);

        endpoints.MapGet($"{apiPath}/texts", TextHandlers.GetPaginatedTexts);
        endpoints.MapPost($"{apiPath}/texts", TextHandlers.CreateText);
        endpoints.MapDelete($"{apiPath}/texts/{{textId}}/{{resourceId}}", TextHandlers.DeleteText);

        endpoints.MapGet($"{apiPath}/translations", TranslationHandlers.GetPaginatedTranslations);
        endpoints.MapPut($"{apiPath}/translations", TranslationHandlers.UpdateTranslation);

        endpoints.MapGet($"{apiPath}/translations/formats", PortabilityHandlers.GetFormats);
        endpoints.MapGet($"{apiPath}/translations/export", PortabilityHandlers.ExportTranslations);
        endpoints.MapPost($"{apiPath}/translations/import", PortabilityHandlers.ImportTranslations);
    }
}
