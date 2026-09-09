using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Portability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using static fbognini.EfCoreLocalization.Dashboard.Helpers.JsonOptions;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Portability;

internal static class PortabilityHandlers
{
    public static async Task GetFormats(HttpContext context)
    {
        var service = context.RequestServices.GetRequiredService<ITranslationsPortabilityService>();

        var formats = service.Formats
            .Select(x => new TranslationsFormatDto
            {
                Name = x.Name,
                ContentType = x.ContentType,
                FileExtension = x.FileExtension
            })
            .ToList();

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, formats, JsonOptions.Default);
    }

    public static async Task ExportTranslations(HttpContext context)
    {
        var service = context.RequestServices.GetRequiredService<ITranslationsPortabilityService>();

        var query = context.Request.Query;

        ITranslationsFormat format;
        try
        {
            format = service.ResolveFormat(query["format"].ToString());
        }
        catch (NotSupportedException exception)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, exception.Message);
            return;
        }

        var filter = new TranslationsExportFilter
        {
            ResourceIds = ReadList(query["resourceIds"]),
            LanguageIds = ReadList(query["languageIds"]),
            ActiveLanguagesOnly = ReadBoolean(query["activeLanguagesOnly"], true),
            IncludeTextsWithoutTranslations = ReadBoolean(query["includeTextsWithoutTranslations"], true)
        };

        var fileName = $"translations-{DateTime.UtcNow:yyyyMMdd-HHmm}{format.FileExtension}";

        // Buffered: the formats write synchronously and Kestrel disallows synchronous writes on the response body.
        using var buffer = new MemoryStream();
        service.Export(buffer, filter, format.Name);
        buffer.Position = 0;

        context.Response.ContentType = format.ContentType;
        context.Response.ContentLength = buffer.Length;
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";

        await buffer.CopyToAsync(context.Response.Body);
    }

    public static async Task ImportTranslations(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "The import expects a multipart form with a file.");
            return;
        }

        var form = await context.Request.ReadFormAsync();
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "No file was uploaded.");
            return;
        }

        var maxSize = context.Items.TryGetValue(DashboardConstants.OptionsItemKey, out var stored) && stored is DashboardOptions options
            ? options.MaxImportFileSizeBytes
            : new DashboardOptions().MaxImportFileSizeBytes;

        if (file.Length > maxSize)
        {
            await WriteError(context, StatusCodes.Status413PayloadTooLarge, $"The file exceeds the {maxSize} bytes limit.");
            return;
        }

        var service = context.RequestServices.GetRequiredService<ITranslationsPortabilityService>();

        // The format defaults to the uploaded file name, so the browser picking an .xlsx is enough.
        var format = form["format"].ToString();
        if (string.IsNullOrWhiteSpace(format))
        {
            format = file.FileName;
        }

        var importOptions = new ImportTranslationsOptions
        {
            DryRun = ReadBoolean(form["dryRun"], false),
            DeleteNotMatched = ReadBoolean(form["deleteNotMatched"], false),
            CreateMissingTexts = ReadBoolean(form["createMissingTexts"], true),
            CreateMissingTranslations = ReadBoolean(form["createMissingTranslations"], true),
            AllowedResourceIds = ReadList(form["allowedResourceIds"])
        };

        try
        {
            using var stream = file.OpenReadStream();
            var result = service.Import(stream, importOptions, format);

            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, result, JsonOptions.Default);
        }
        catch (NotSupportedException exception)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (InvalidDataException exception)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (ArgumentException exception)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (LocalizationImportException exception)
        {
            await WriteError(context, StatusCodes.Status409Conflict, exception.Message);
        }
    }

    private static async Task WriteError(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = message }, JsonOptions.Default);
    }

    private static List<string>? ReadList(StringValues values)
    {
        var items = values
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();

        return items.Count == 0 ? null : items;
    }

    private static bool ReadBoolean(StringValues values, bool fallback)
    {
        var value = values.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return bool.TryParse(value, out var parsed) ? parsed : value == "1" || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
    }
}
