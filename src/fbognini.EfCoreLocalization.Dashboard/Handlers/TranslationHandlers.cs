using fbognini.Core.Domain.Query;
using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Translations;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using static fbognini.EfCoreLocalization.Dashboard.Helpers.JsonOptions;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers;

internal static class TranslationHandlers
{
    public static async Task GetPaginatedTranslations(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        var queryString = context.Request.Query;
        var page = int.TryParse(queryString["page"], out var p) ? p : 1;
        var pageSize = int.TryParse(queryString["pageSize"], out var ps) ? ps : 10;
        var search = queryString["search"].ToString();
        var languageId = queryString["languageId"].ToString();
        var textId = queryString["textId"].ToString();
        var resourceId = queryString["resourceId"].ToString();
        var onlyNotTranslated = bool.TryParse(queryString["onlyNotTranslated"], out var ont) && ont;

        var criteria = new QueryableCriteria<Translation>
        {
            //Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
            //Search = search
        };

        var result = repository.GetPaginatedTranslations(criteria);
        
        // Apply additional filters
        var items = result.Items.AsEnumerable();
        if (!string.IsNullOrEmpty(languageId))
            items = items.Where(t => t.LanguageId == languageId);
        if (!string.IsNullOrEmpty(textId))
            items = items.Where(t => t.TextId == textId);
        if (!string.IsNullOrEmpty(resourceId))
            items = items.Where(t => t.ResourceId == resourceId);
        if (onlyNotTranslated)
            items = items.Where(t => string.IsNullOrWhiteSpace(t.Destination));

        var response = new PaginationResponse<TranslationDto>
        {
            Pagination = result.Pagination,
            Items = items.Select(t => TranslationMappings.ToDto(t)).ToList()
        };

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions.Default);
    }

    public static async Task UpdateTranslation(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var command = JsonSerializer.Deserialize<UpdateTranslationCommand>(body, JsonOptions.Default);

        if (command == null)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var translation = repository.GetTranslation(command.LanguageId, command.TextId, command.ResourceId);
        if (translation == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        translation.Destination = command.Destination;
        repository.UpdateTranslation(translation);

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, TranslationMappings.ToDto(translation), JsonOptions.Default);
    }

    public static async Task UpdateTranslations(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var commands = JsonSerializer.Deserialize<List<UpdateTranslationCommand>>(body, JsonOptions.Default);

        if (commands == null || commands.Count == 0)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var translations = new List<Translation>();
        foreach (var command in commands)
        {
            var translation = repository.GetTranslation(command.LanguageId, command.TextId, command.ResourceId);
            if (translation != null)
            {
                translation.Destination = command.Destination;
                translations.Add(translation);
            }
        }

        if (translations.Count > 0)
        {
            repository.UpdateTranslations(translations);
        }

        context.Response.StatusCode = 204;
    }
}
