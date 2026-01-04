# fbognini.EfCoreLocalization

A simple and flexible localization library for ASP.NET Core that stores translations in your database using Entity Framework Core. No more resource files to manage - everything lives in your database where you can easily update translations without redeploying your application.

## What's included

This package consists of two NuGet packages:

- **fbognini.EfCoreLocalization** - the core library that provides database-backed localization
- **fbognini.EfCoreLocalization.Dashboard** - an optional web dashboard to manage translations through a UI

## Installation

Install the core package:

```bash
dotnet add package fbognini.EfCoreLocalization
```

If you want the dashboard (recommended for managing translations):

```bash
dotnet add package fbognini.EfCoreLocalization.Dashboard
```

## Quick start

### 1. configure your database context

First, add the `EfCoreLocalizationDbContext` to your services. You'll need to configure it with your database connection:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<EfCoreLocalizationDbContext>(options => 
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("YourAppName")), 
    ServiceLifetime.Singleton, 
    ServiceLifetime.Singleton);
```

### 2. Add localization services

Add the localization services to your DI container:

```csharp
builder.Services.AddRazorPages()
    .AddViewLocalization(); // or AddMvc().AddViewLocalization() for MVC

builder.Services.AddLocalization();
builder.Services.AddEfCoreLocalization(builder.Configuration);
```

### 3. Configure settings (optional)

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

- **DefaultSchema** - database schema name for localization tables (optional)
- **GlobalResourceId** - if set, all translations use this ResourceId. Only TextId is used to find translations
- **ResourceIdPrefix** - prefix to add to all ResourceIds
- **RemovePrefixs** - list of prefixes to remove from ResourceIds
- **RemoveSuffixs** - list of suffixes to remove from ResourceIds
- **ReturnOnlyKeyIfNotFound** - if true, returns just the key when translation is missing. If false, returns the full search key
- **CreateNewRecordWhenDoesNotExists** - automatically creates a new translation record in the database if one doesn't exist

### 4. Create and apply migrations

First, create the migration. From the command line:

```bash
dotnet ef migrations add Localization --context EfCoreLocalizationDbContext
```

Or from the Package Manager Console in Visual Studio:

```powershell
Add-Migration Localization -Context EfCoreLocalizationDbContext
```

Then apply the migrations. You can do it automatically when your app starts:

```csharp
var app = builder.Build();

await app.ApplyMigrationEFCoreLocalization();
```

Or apply them manually from the command line:

```bash
dotnet ef database update --context EfCoreLocalizationDbContext
```

Or from the Package Manager Console:

```powershell
Update-Database -Context EfCoreLocalizationDbContext
```

This will create the localization tables in your database.

### 5. Configure request localization

Set up request localization to use languages from your database:

```csharp
app.UseRequestLocalizationWithEFCoreLocalization();
```

### 6. Localize your app!

In your Razor views, inject `IViewLocalizer` and use it:

```razor
@using Microsoft.AspNetCore.Mvc.Localization
@inject IViewLocalizer Localizer

<h1>@Localizer["Welcome"]</h1>
<p>@Localizer["HeroText"]</p>
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
        var welcomeMessage = _localizer["Welcome"];
    }
}
```

## Using the dashboard

The dashboard provides a web UI to manage your translations without touching the database directly.

### Setup

Add the dashboard services:

```csharp
builder.Services.AddEfCoreLocalizationDashboard();
```

Then add the dashboard middleware. If you're using MVC, make sure to register your controller routes first:

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
var dashboardOptions = new DashboardOptions();
app.UseEfCoreLocalizationDashboard(options: dashboardOptions);

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

- **Languages** - the languages you support (e.g., "en", "it", "fr")
- **Texts** - the text keys you want to translate (identified by `TextId` and `ResourceId`)
- **Translations** - the actual translated text for each language

When you call `Localizer["MyKey"]`, the library:
1. Looks up the current culture from `CultureInfo.CurrentCulture`
2. Searches for a translation matching the key and culture
3. Returns the translated text, or the key itself if not found (depending on your settings)


## Customizing Resource IDs

By default, the ResourceId is derived from the type name. For example, `IndexModel` becomes `"IndexModel"`. You can customize this using the `LocalizationKeyAttribute`:

```csharp
[LocalizationKey("MyCustomResource")]
public class IndexModel : PageModel
{
    // ...
}
```

## Example project

Check out the `SampleWebApp` project in the repository for a complete working example.

## Requirements

- .NET 8.0 or later
- Entity Framework Core 8.0
- SQL Server (or any EF Core supported database)

## Acknowledgments

This code was freely inspired by [AspNetCoreLocalization](https://github.com/damienbod/AspNetCoreLocalization) by damienbod.
