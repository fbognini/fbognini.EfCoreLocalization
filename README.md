# EFCore 

[![NuGet](https://img.shields.io/nuget/v/fbognini.EfCoreLocalization.svg)](https://www.nuget.org/packages/fbognini.EfCoreLocalization/)
[![Relaease](https://github.com/fbognini/fbognini.EfCoreLocalization/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/fbognini/fbognini.EfCoreLocalization/actions?query=event%3Arelease)

A flexible, database-driven localization provider for ASP.NET Core using Entity Framework Core. This library eliminates the need for static resource files, allowing dynamic management of translations without application redeployment.

> [!NOTE]
> This library replaces [fbognini.i18n](https://github.com/fbognini/fbognini.i18n). The `fbognini.i18n` and `fbognini.i18n.Dashboard` NuGet packages are deprecated and no longer maintained: see [Migrating from fbognini.i18n](#migrating-from-fbogninii18n).

## What's included

This package consists of three NuGet packages:

- **fbognini.EfCoreLocalization** - The core library that provides database-backed localization, including CSV export and import
- **fbognini.EfCoreLocalization.Dashboard** - An optional web dashboard to manage translations through a UI
- **fbognini.EfCoreLocalization.Excel** - An optional xlsx export and import format

## Installation

Install the core library:

```bash
dotnet add package fbognini.EfCoreLocalization
```

Optionally, install the management dashboard:

```bash
dotnet add package fbognini.EfCoreLocalization.Dashboard
```

Optionally, install the Excel export and import format:

```bash
dotnet add package fbognini.EfCoreLocalization.Excel
```

## Quick start

### 1. Configure your database context

Register `EfCoreLocalizationDbContext` to your services. You'll need to configure it with your database connection:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<EfCoreLocalizationDbContext>(options => 
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("YourAppName")), 
    ServiceLifetime.Singleton, 
    ServiceLifetime.Singleton);
```

### 2. Register services

Add the localization services to your DI container:

```csharp
builder.Services.AddLocalization();
builder.Services.AddEfCoreLocalization(builder.Configuration);
```

### 3. Apply migrations

Generate and apply the necessary database tables.

#Via .NET CLI:

```bash
dotnet ef migrations add Localization --context EfCoreLocalizationDbContext
dotnet ef database update --context EfCoreLocalizationDbContext
```

Via Package Manager Console in Visual Studio:

```powershell
Add-Migration Localization -Context EfCoreLocalizationDbContext
Update-Database -Context EfCoreLocalizationDbContext
```

Alternatively, apply migrations programmatically at startup:

```csharp
var app = builder.Build();

await app.ApplyMigrationEFCoreLocalization();
```

### 4. Configure middleware

Enable request localization using the database settings.

```csharp
var app = builder.Build();

app.UseRequestLocalizationWithEFCoreLocalization();
```

## Configuration

You can configure localization settings in your `appsettings.json`:

```json
{
  "EfCoreLocalization": {
    "DefaultSchema": "localization",
    "ReturnOnlyKeyIfNotFound": true,
    "CreateNewRecordWhenDoesNotExists": true,
    "GlobalResourceId": null,
    "ResourceIdPrefix": null,
    "RemovePrefixsFromTypes": [],
    "RemoveSuffixsFromTypes": ["Dto"],
    "IgnoreResourceLocation": false,
    "RemovePrefixsFromLocations": [],
    "CacheExpirationMinutes": 30
  }
}
```

Or configure it in code:

```csharp
builder.Services.AddEfCoreLocalization(options =>
{
    options.DefaultSchema = "localization";
    options.ReturnOnlyKeyIfNotFound = true;
    options.CreateNewRecordWhenDoesNotExists = true;
    options.CacheExpirationMinutes = 30; // Cache expires after 30 minutes, or null for infinite cache
});
```

### Reference

| Option | Type | Description |
| --- | --- | --- |
| DefaultSchema | string? | The database schema for the localization tables. If empty, the provider default schema is used. |
| GlobalResourceId | string? | If set, it is used as the ResourceId for every lookup, `[LocalizationKey]` included. |
| ResourceIdPrefix | string? | Prefix prepended, dot separated, to the computed ResourceId. |
| RemovePrefixsFromTypes | string[] | Prefixes stripped from the type name when it is used as ResourceId. |
| RemoveSuffixsFromTypes | string[] | Suffixes stripped from the type name when it is used as ResourceId. |
| IgnoreResourceLocation | bool | If `true`, the location is not prepended to the base name when the ResourceId is built from a base name and a location, as it happens in views. |
| RemovePrefixsFromLocations | string[] | Prefixes stripped from the `location.baseName` ResourceId. |
| ReturnOnlyKeyIfNotFound | bool | If `true`, returns the key when a translation is missing, otherwise returns the full search key (`ResourceId.Key.Culture`). |
| CreateNewRecordWhenDoesNotExists | bool | If `true`, automatically inserts missing keys into the database. |
| CacheExpirationMinutes | int? | Cache expiration time in minutes. If `null`, the cache never expires (infinite). |

> [!WARNING] 
> Make sure to re-apply and run migrations if you change the `DefaultSchema`.

## Usage

Use standard ASP.NET Core interfaces (`IViewLocalizer`, `IStringLocalizer<T>`).

In your Razor views, inject `IViewLocalizer` and use it:

```razor
@using Microsoft.AspNetCore.Mvc.Localization
@inject IViewLocalizer Localizer

<h1>@Localizer["Welcome"]</h1>
```

Or in your code-behind/controllers:

```csharp
public class IndexModel : PageModel
{
    private readonly IStringLocalizer<IndexModel> _localizer;

    public IndexModel(IStringLocalizer<IndexModel> localizer)
    {
        _localizer = localizer;
    }

    public void OnGet()
    {
        var message = _localizer["Welcome"];
    }
}

```


### Customizing Resource IDs

By default, the Resource ID matches the type name (e.g., "IndexModel"). You can customize this using the `LocalizationKeyAttribute`:

```csharp
[LocalizationKey("MyCustomResource")]
public class IndexModel : PageModel
{
    // ...
}
```

## Dashboard

The Dashboard provides a UI to manage translations.

### Setup

Add the dashboard middleware. If you're using MVC, make sure to register your controller routes first:

```csharp
var app = builder.Build();

// For MVC apps, register routes before the dashboard
app.MapControllerRoute(
    name: "area",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add the dashboard
app.UseEfCoreLocalizationDashboard();

app.MapRazorPages(); // or your other route mappings
```

The dashboard will be available at `/localization` by default. You can change the path:

```csharp
app.UseEfCoreLocalizationDashboard(pathMatch: "my-custom-path", options: dashboardOptions);
```

### Authorization

By default, the dashboard only allows requests from localhost. You can customize this:

```csharp
var dashboardOptions = new DashboardOptions
{
    Authorization = new[] { new YourCustomAuthorizationFilter() },
    AsyncAuthorization = new[] { new YourAsyncAuthorizationFilter() }
};
```

> [!WARNING]
> The dashboard exposes an import endpoint that can delete keys in bulk. If you replace the default localhost
> filter, make sure the replacement is at least as strict.

## Export & import

Translations can be exported to a file, handed to a translator, and imported back, getting the counts of
what was added, updated and deleted — numbers meant for your own audit log.

The file uses a **wide** layout: one row per key, one column per language.

| ResourceId | TextId | Description | it-IT | en-US |
|---|---|---|---|---|
| dashboard | Home.Title | Page title | Benvenuto | Welcome |
| dashboard | Home.Body | | Corpo | |

CSV is built into the core package. For Excel, install the optional package and register the format:

```bash
dotnet add package fbognini.EfCoreLocalization.Excel
```

```csharp
builder.Services.AddEfCoreLocalizationExcel();
```

Both are consumed through `ITranslationsPortabilityService`, which picks the format, applies the file and
refreshes the localizer cache:

```csharp
app.MapGet("/translations/export", (ITranslationsPortabilityService service, string? format) =>
{
    var translationsFormat = service.ResolveFormat(format);   // "csv", "xlsx", "excel", ".xlsx" or a file name

    var buffer = new MemoryStream();
    service.Export(buffer, new TranslationsExportFilter { ResourceIds = ["dashboard"] }, translationsFormat.Name);
    buffer.Position = 0;

    return Results.File(buffer, translationsFormat.ContentType, $"translations{translationsFormat.FileExtension}");
});

app.MapPost("/translations/import", async (ITranslationsPortabilityService service, IFormFile file) =>
{
    await using var stream = file.OpenReadStream();

    var result = service.Import(stream, new ImportTranslationsOptions(), file.FileName);

    logger.LogInformation("Import: {Created} keys created, {Updated} translations updated, {Deleted} keys deleted",
        result.TextsCreated, result.TranslationsUpdated, result.TextsDeleted);

    return Results.Ok(result);
}).DisableAntiforgery();
```

The same flow is available from the Dashboard, under *Translations → Export / Import*.

### Import options

| Option | Default | Description |
|---|---|---|
| `CreateMissingTexts` | `true` | Creates keys that are in the file but not in the database |
| `CreateMissingTranslations` | `true` | Creates translations that are in the file but not in the database |
| `DeleteNotMatched` | `false` | Deletes keys that are in the database but not in the file, **limited to the resource ids the file contains** |
| `DryRun` | `false` | Computes and returns the counters without writing anything |
| `AllowedResourceIds` | `null` | Rejects rows whose resource id is outside this list |

**An empty cell means "no value supplied", never "delete"**: the stored translation is left untouched and
counted in `TranslationsSkipped`. Deletions only ever happen through `DeleteNotMatched`.

> [!WARNING]
> `DeleteNotMatched` deletes every key of the exported resource ids that is missing from the file. If the export
> was filtered, everything filtered out is deleted. Run the import with `DryRun = true` first and check
> `TextsDeleted` before applying it — that is exactly what the Dashboard does.

Rejected rows do not stop the others: the rest is imported and every problem is reported in
`ImportTranslationsResult.Errors` with its source row, its kind and a reason. Only a structurally unusable file
throws — one whose language columns match no language at all.

### Localizer cache

An import that changed something drops the localizer cache, so the new texts are served right away. The cache is
per process: if you run more than one instance, set `CacheExpirationMinutes` so that the others pick the changes
up as well. Adding a *language* still requires a restart, because the supported cultures are read once at startup.

## How it works

The library stores translations in three main tables:

- **Languages** - The languages you support (e.g., "en-US", "it-IT", "fr-FR")
- **Texts** - The text keys you want to translate (identified by `TextId` and `ResourceId`)
- **Translations** - The actual translated text for each language

When you call `Localizer["MyKey"]`, the library:
1. Looks up the current culture from `CultureInfo.CurrentCulture`
2. Searches for a translation matching the key and culture
3. Returns the translated text, or the key itself if not found (depending on your settings)

### Caching

Translations are cached in memory to improve performance. By default, the cache never expires. You can configure cache expiration using `CacheExpirationMinutes`:

- Set to `null` (default): Cache never expires, translations are loaded once and kept in memory
- Set to a number (e.g., `30`): Cache expires after the specified number of minutes, forcing a reload from the database

The cache is checked lazily - expiration is verified when accessing a resource, not on a timer. 

## Example project

Check out the `SampleWebApp` project in the repository for a complete working example.

## Migrating from fbognini.i18n

This library is the successor of [fbognini.i18n](https://github.com/fbognini/fbognini.i18n).

| Deprecated package | Use instead |
| --- | --- |
| [fbognini.i18n](https://www.nuget.org/packages/fbognini.i18n/) | [fbognini.EfCoreLocalization](https://www.nuget.org/packages/fbognini.EfCoreLocalization/) |
| [fbognini.i18n.Dashboard](https://www.nuget.org/packages/fbognini.i18n.Dashboard/) | [fbognini.EfCoreLocalization.Dashboard](https://www.nuget.org/packages/fbognini.EfCoreLocalization.Dashboard/) |

Replace the namespace `fbognini.i18n` with `fbognini.EfCoreLocalization`, then update the API and the settings.

| fbognini.i18n | fbognini.EfCoreLocalization |
| --- | --- |
| `AddI18N(...)` | `AddLocalization()` + `AddEfCoreLocalization(...)` |
| `InitializeI18N()` | `ApplyMigrationEFCoreLocalization()` |
| `UseRequestLocalizationI18N()` | `UseRequestLocalizationWithEFCoreLocalization()` |
| `UseI18nDashboard()` | `UseEfCoreLocalizationDashboard()` |
| `I18nContext` | `EfCoreLocalizationDbContext` |
| `II18nRepository` | `ILocalizationRepository` |
| `[I18NKey]` | `[LocalizationKey]` |

The configuration section is now named `EfCoreLocalization` instead of `I18nSettings`, and the settings are flat.

| I18nSettings | EfCoreLocalizationSettings |
| --- | --- |
| `ConnectionString` | removed, configured on `AddDbContext<EfCoreLocalizationDbContext>` |
| `Schema` | `DefaultSchema` |
| `UseCache` | removed, the cache is always enabled and tuned with `CacheExpirationMinutes` |
| `CookieName` | removed, register your own `CookieRequestCultureProvider` on `RequestLocalizationOptions` |
| `Localizer.OverrideResourceId` | `GlobalResourceId` |
| `Localizer.BaseResourceId` | `ResourceIdPrefix` |
| `Localizer.RemovePrefixs` | `RemovePrefixsFromTypes` |
| `Localizer.RemoveSuffixs` | `RemoveSuffixsFromTypes` |
| `Localizer.CreateNewRecordWhenDoesNotExists` | `CreateNewRecordWhenDoesNotExists` |

The `Languages`, `Texts` and `Translations` tables kept the same structure: only the schema changed, `Texts.Created` became `Texts.CreatedOnUtc`, `Translations.Updated` became `Translations.UpdatedOnUtc` and the `Configurations` table is no longer used. The full step by step guide, including the SQL to copy the existing rows, is in the [fbognini.i18n readme](https://github.com/fbognini/fbognini.i18n#migration-guide).

## Requirements

- .NET 8.0 or later
- Entity Framework Core 8.0 or later
- A database provider compatible with EF Core

## Acknowledgments

This code was freely inspired by [AspNetCoreLocalization](https://github.com/damienbod/AspNetCoreLocalization) by damienbod.
