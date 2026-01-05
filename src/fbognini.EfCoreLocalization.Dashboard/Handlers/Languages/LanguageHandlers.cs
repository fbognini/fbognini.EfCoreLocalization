using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Languages;

internal static class LanguageHandlers
{

    public static async Task GetPaginatedLanguages(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();

        var criteria = new LanguageSelectCriteria();

        var fullSearchParams = await FullSearchHelper.BindFromQueryAsync(context);
        if (fullSearchParams != null)
        {
            criteria.LoadFullSearch(fullSearchParams.ToFullSearch());
            criteria.Search.Fields.Add(x => x.Id);
            criteria.Search.Fields.Add(x => x.Description);
        }

        var result = repository.GetPaginatedLanguages(criteria);
        var response = new PaginationResponse<LanguageDto>
        {
            Pagination = result.Pagination,
            Items = result.Items.Select(l => l.ToDto()).ToList()
        };

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions.Default);
    }

    public static async Task CreateLanguage(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var command = JsonSerializer.Deserialize<CreateLanguageCommand>(body, JsonOptions.Default);

        if (command == null)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var language = new Language
        {
            Id = command.Id,
            Description = command.Description,
            IsActive = command.IsActive,
            IsDefault = command.IsDefault
        };

        repository.AddLanguage(language);

        context.Response.StatusCode = 201;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, language.ToDto(), JsonOptions.Default);
    }

    public static async Task UpdateLanguage(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        var id = context.Request.RouteValues["id"]?.ToString();

        if (string.IsNullOrEmpty(id))
        {
            context.Response.StatusCode = 400;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var command = JsonSerializer.Deserialize<UpdateLanguageCommand>(body, JsonOptions.Default);

        if (command == null)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var language = repository.UpdateLanguage(id, command.Description, command.IsActive, command.IsDefault);

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, language.ToDto(), JsonOptions.Default);
    }

    private static LanguageDto ToDto(this Language language) => new LanguageDto()
    {
        Id = language.Id,
        Description = language.Description,
        IsActive = language.IsActive,
        IsDefault = language.IsDefault
    };
}
