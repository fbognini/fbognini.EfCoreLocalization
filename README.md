# EFCore 

[![NuGet](https://img.shields.io/nuget/v/fbognini.EfCoreLocalization.svg)](https://www.nuget.org/packages/fbognini.EfCoreLocalization/)
[![Relaease](https://github.com/fbognini/fbognini.EfCoreLocalization/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/fbognini/fbognini.EfCoreLocalization/actions?query=event%3Arelease)

A flexible, database-driven localization provider for ASP.NET Core using Entity Framework Core. This library eliminates the need for static resource files, allowing dynamic management of translations without application redeployment.

## What's included

This package consists of two NuGet packages:

- **fbognini.EfCoreLocalization** - The core library that provides database-backed localization
- **fbognini.EfCoreLocalization.Dashboard** - An optional web dashboard to manage translations through a UI

## Installation

Install the core library:

```bash
dotnet add package fbognini.EfCoreLocalization
```

Optionally, install the management dashboard:

```bash
dotnet add package fbognini.EfCoreLocalization.Dashboard
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

// ... other middleware

app.UseRequestLocalizationWithEFCoreLocalization();

// ... routing and endpoints
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
    "RemovePrefixs": [],
    "RemoveSuffixs": ["Dto"]
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
});
```

### Reference

| Option                           | Type      | Description                                                           |
|----------------------------------|-----------|-----------------------------------------------------------------------|
| DefaultSchema                    | string    | The database schema for localization tables.                           |
| GlobalResourceId                 | string?   | If set, overrides the ResourceId for all lookups.                  |
| ReturnOnlyKeyIfNotFound          | bool      | If `true`, returns the key string when a translation is missing.       |
| CreateNewRecordWhenDoesNotExists | bool      | If `true`, automatically inserts missing keys into the database.       |
| RemovePrefixs                    | string[]  | List of prefixes to strip from the ResourceId.                |
| RemoveSuffixs                    | string[]  | List of suffixes to strip from the ResourceId.                |


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

## How it works

The library stores translations in three main tables:

- **Languages** - The languages you support (e.g., "en-US", "it-IT", "fr-FR")
- **Texts** - The text keys you want to translate (identified by `TextId` and `ResourceId`)
- **Translations** - The actual translated text for each language

When you call `Localizer["MyKey"]`, the library:
1. Looks up the current culture from `CultureInfo.CurrentCulture`
2. Searches for a translation matching the key and culture
3. Returns the translated text, or the key itself if not found (depending on your settings)


## Example project

Check out the `SampleWebApp` project in the repository for a complete working example.

## Requirements

- .NET 8.0 or later
- Entity Framework Core 8.0 or later
- A database provider compatible with EF Core

## Acknowledgments

This code was freely inspired by [AspNetCoreLocalization](https://github.com/damienbod/AspNetCoreLocalization) by damienbod.