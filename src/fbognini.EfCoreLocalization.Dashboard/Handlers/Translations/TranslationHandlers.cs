using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Translations;

internal static class TranslationHandlers
{
    public static async Task GetPaginatedTranslations(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        var queryString = context.Request.Query;
        var languageId = queryString["languageId"].ToString();
        var textId = queryString["textId"].ToString();
        var resourceId = queryString["resourceId"].ToString();
        var onlyNotTranslated = bool.TryParse(queryString["onlyNotTranslated"], out var ont) && ont;

        var criteria = new TranslationSelectCriteria
        {
            LanguageId = !string.IsNullOrEmpty(languageId) ? languageId : null,
            TextId = !string.IsNullOrEmpty(textId) ? textId : null,
            ResourceId = !string.IsNullOrEmpty(resourceId) ? resourceId : null,
            NotTranslated = onlyNotTranslated ? true : null
        };

        var fullSearchParams = await FullSearchHelper.BindFromQueryAsync(context);
        if (fullSearchParams != null)
        {
            criteria.LoadFullSearch(fullSearchParams.ToFullSearch());
            criteria.Search.Fields.Add(x => x.TextId);
            criteria.Search.Fields.Add(x => x.ResourceId);
            criteria.Search.Fields.Add(x => x.Destination);
        }

        var result = repository.GetPaginatedTranslations(criteria);
        var response = new PaginationResponse<TranslationDto>
        {
            Pagination = result.Pagination,
            Items = result.Items.Select(t => ToDto(t)).ToList()
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
        await JsonSerializer.SerializeAsync(context.Response.Body, ToDto(translation), JsonOptions.Default);
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

    private static TranslationDto ToDto(this Translation translation)
    {
        var dto = new TranslationDto()
        {
            LanguageId = translation.LanguageId,
            TextId = translation.TextId,
            ResourceId = translation.ResourceId,
            Destination = translation.Destination,
            UpdatedOnUtc = translation.UpdatedOnUtc,
        };

        return dto;
    }
}
