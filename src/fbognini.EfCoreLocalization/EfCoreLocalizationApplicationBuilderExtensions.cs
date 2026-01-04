using fbognini.EfCoreLocalization.Localizers;
using fbognini.EfCoreLocalization.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization
{
    public static class EfCoreLocalizationApplicationBuilderExtensions
    {
        public static async Task ApplyMigrationEFCoreLocalization(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<EfCoreLocalizationDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync(cancellationToken);
            }
        }

        public static async Task<IApplicationBuilder> ApplyMigrationEFCoreLocalization(this IApplicationBuilder app, CancellationToken cancellationToken = default)
        {
            using var scope = app.ApplicationServices.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<EfCoreLocalizationDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync(cancellationToken);
            }

            return app;
        }

        public static IApplicationBuilder UseRequestLocalizationWithEFCoreLocalization(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<ILocalizationRepository>();
            var languages = service.GetLanguages().Where(x => x.IsActive).ToList();
            if (languages.Count != 0)
            {
                var defaultCulture = languages.FirstOrDefault(x => x.IsDefault) ?? languages.First();

                var options = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value
                    .SetDefaultCulture(defaultCulture.Id)
                    .AddSupportedCultures([.. languages.Select(x => x.Id)])
                    .AddSupportedUICultures([.. languages.Select(x => x.Id)]);

                app.UseRequestLocalization(options);
            }

            return app;
        }
    }
}
