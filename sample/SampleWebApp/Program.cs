using fbognini.EfCoreLocalization;
using fbognini.EfCoreLocalization.Dashboard;
using fbognini.EfCoreLocalization.Dashboard.Extensions;
using fbognini.EfCoreLocalization.Excel;
using fbognini.EfCoreLocalization.Portability;
using fbognini.EfCoreLocalization.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();
builder.Services.AddRazorPages()
    .AddViewLocalization();


var cs = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<EfCoreLocalizationDbContext>(options => options.UseSqlServer(cs, b => b.MigrationsAssembly("SampleWebApp")), ServiceLifetime.Singleton, ServiceLifetime.Singleton);

builder.Services.AddEfCoreLocalization(builder.Configuration);
builder.Services.AddEfCoreLocalizationExcel();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


await app.ApplyMigrationEFCoreLocalization();

app.UseRequestLocalizationWithEFCoreLocalization();

var dashboardOptions = new DashboardOptions() { };
app.UseEfCoreLocalizationDashboard(options: dashboardOptions);

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/translations/export", (ITranslationsPortabilityService service, string? format, string? resourceIds) =>
{
    var translationsFormat = service.ResolveFormat(format);
    var filter = new TranslationsExportFilter
    {
        ResourceIds = resourceIds?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    };

    var buffer = new MemoryStream();
    service.Export(buffer, filter, translationsFormat.Name);
    buffer.Position = 0;

    return Results.File(buffer, translationsFormat.ContentType, $"translations{translationsFormat.FileExtension}");
});

app.MapPost("/translations/import", async (ITranslationsPortabilityService service, IFormFile file, bool dryRun = false, bool deleteNotMatched = false) =>
{
    await using var stream = file.OpenReadStream();
    var options = new ImportTranslationsOptions { DryRun = dryRun, DeleteNotMatched = deleteNotMatched };

    return Results.Ok(service.Import(stream, options, file.FileName));
}).DisableAntiforgery();

app.Run();
