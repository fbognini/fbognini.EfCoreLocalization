using fbognini.Core.Domain.Query;
using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Languages;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using static fbognini.EfCoreLocalization.Dashboard.Helpers.JsonOptions;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers;

internal static class LanguageHandlers
{
    public static async Task GetLanguages(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        var languages = repository.GetLanguages().Select(l => l.ToDto()).ToList();

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, languages, JsonOptions.Default);
    }

    public static async Task GetPaginatedLanguages(HttpContext context)
    {
        var repository = context.RequestServices.GetRequiredService<ILocalizationRepository>();
        
        var queryString = context.Request.Query;
        var page = int.TryParse(queryString["page"], out var p) ? p : 1;
        var pageSize = int.TryParse(queryString["pageSize"], out var ps) ? ps : 10;
        var search = queryString["search"].ToString();

        var criteria = new QueryableCriteria<Language>
        {
            //Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
            //Search = search
        };

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
}
