using fbognini.Core.Domain.Query;
using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Texts;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using static fbognini.EfCoreLocalization.Dashboard.Helpers.JsonOptions;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers;

internal static class TextHandlers
{
    public static async Task GetPaginatedTexts(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        var queryString = context.Request.Query;
        var page = int.TryParse(queryString["page"], out var p) ? p : 1;
        var pageSize = int.TryParse(queryString["pageSize"], out var ps) ? ps : 10;
        var search = queryString["search"].ToString();
        var textId = queryString["textId"].ToString();
        var resourceId = queryString["resourceId"].ToString();

        var criteria = new QueryableCriteria<Text>
        {
            //Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
            //Search = search
        };

        // Apply filters if provided
        if (!string.IsNullOrEmpty(textId) || !string.IsNullOrEmpty(resourceId))
        {
            // Note: This would need to be handled in the repository or via criteria
            // For now, we'll pass it through the criteria
        }

        var result = repository.GetPaginatedTexts(criteria);
        var response = new PaginationResponse<TextDto>
        {
            Pagination = result.Pagination,
            Items = result.Items.Select(t => TextMappings.ToDto(t)).ToList()
        };

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions.Default);
    }

    public static async Task CreateText(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var command = JsonSerializer.Deserialize<CreateTextCommand>(body, JsonOptions.Default);

        if (command == null || string.IsNullOrEmpty(command.TextId) || string.IsNullOrEmpty(command.ResourceId))
        {
            context.Response.StatusCode = 400;
            return;
        }

        // Create translations for all languages with empty values
        var languages = repository.GetLanguages().Where(l => l.IsActive).ToList();
        var translations = languages.ToDictionary(l => l.Id, _ => string.Empty);

        repository.AddTranslations(command.TextId, command.ResourceId, command.Description ?? string.Empty, translations);

        context.Response.StatusCode = 201;
    }

    public static async Task DeleteText(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        var textId = context.Request.RouteValues["textId"]?.ToString();
        var resourceId = context.Request.RouteValues["resourceId"]?.ToString();

        if (string.IsNullOrEmpty(textId) || string.IsNullOrEmpty(resourceId))
        {
            context.Response.StatusCode = 400;
            return;
        }

        repository.DeleteTranslations(textId, resourceId);
        context.Response.StatusCode = 204;
    }
}
