using fbognini.Core.Domain.Query;
using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using static fbognini.EfCoreLocalization.Dashboard.Helpers.JsonOptions;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Texts;

internal static class TextHandlers
{
    public static async Task GetPaginatedTexts(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();

        var queryString = context.Request.Query;
        var textId = queryString["textId"].ToString();
        var resourceId = queryString["resourceId"].ToString();

        var criteria = new TextSelectCriteria
        {
            TextId = !string.IsNullOrEmpty(textId) ? textId : null,
            ResourceId = !string.IsNullOrEmpty(resourceId) ? resourceId : null
        };

        var fullSearchParams = await FullSearchHelper.BindFromQueryAsync(context);
        if (fullSearchParams != null)
        {
            criteria.LoadFullSearch(fullSearchParams.ToFullSearch());
            criteria.Search.Fields.Add(x => x.TextId);
            criteria.Search.Fields.Add(x => x.ResourceId);
            criteria.Search.Fields.Add(x => x.Description);
        }

        var result = repository.GetPaginatedTexts(criteria);
        var response = new PaginationResponse<TextDto>
        {
            Pagination = result.Pagination,
            Items = result.Items.Select(t => ToDto(t)).ToList()
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

    private static TextDto ToDto(this Text text) => new TextDto()
    {
        TextId = text.TextId,
        ResourceId = text.ResourceId,
        Description = text.Description,
        CreatedOnUtc = text.CreatedOnUtc,
    };
}
