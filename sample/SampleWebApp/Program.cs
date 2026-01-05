using fbognini.EfCoreLocalization;
using fbognini.EfCoreLocalization.Dashboard;
using fbognini.EfCoreLocalization.Dashboard.Extensions;
using fbognini.EfCoreLocalization.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLocalization();
builder.Services.AddRazorPages()
    .AddViewLocalization();


var cs = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<EfCoreLocalizationDbContext>(options => options.UseSqlServer(cs, b => b.MigrationsAssembly("SampleWebApp")), ServiceLifetime.Singleton, ServiceLifetime.Singleton);

builder.Services.AddEfCoreLocalization(builder.Configuration);
//builder.Services.AddEfCoreLocalizationDashboard();

var app = builder.Build();


// Configure the HTTP request pipeline.
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
app.UseLocalizationDashboard(options: dashboardOptions);

app.UseAuthorization();

app.MapRazorPages();

app.Run();
