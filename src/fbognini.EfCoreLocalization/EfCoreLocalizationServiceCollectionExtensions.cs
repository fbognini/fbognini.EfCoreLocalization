using fbognini.EfCoreLocalization.Localizers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Portability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using System;

namespace fbognini.EfCoreLocalization
{
    public static class EfCoreLocalizationServiceCollectionExtensions
    {
        public static IServiceCollection AddEfCoreLocalization(this IServiceCollection services, Action<EfCoreLocalizationSettings> options)
        {
            var settings = new EfCoreLocalizationSettings();
            options.Invoke(settings);

            services.Configure(options);

            return services.AddEfCoreLocalization(settings);
        }

        public static IServiceCollection AddEfCoreLocalization(this IServiceCollection services, IConfiguration configuration, Action<EfCoreLocalizationSettings>? options = null)
        {
            return services.AddEfCoreLocalization(configuration.GetSection("EfCoreLocalization"), options);
        }

        public static IServiceCollection AddEfCoreLocalization(this IServiceCollection services, IConfigurationSection section, Action<EfCoreLocalizationSettings>? options = null)
        {
            var settings = section.Get<EfCoreLocalizationSettings>() ?? new EfCoreLocalizationSettings();

            services.Configure<EfCoreLocalizationSettings>(section);

            if (options != null)
            {
                options(settings);
                services.PostConfigure<EfCoreLocalizationSettings>(setting =>
                {
                    options(setting);
                });
            }

            return services.AddEfCoreLocalization(settings);
        }

        private static IServiceCollection AddEfCoreLocalization(this IServiceCollection services, EfCoreLocalizationSettings settings)
        {
            services.AddSingleton<ILocalizationRepository, LocalizationRepository>();
            services.AddSingleton<EFStringLocalizerFactory>();
            services.AddSingleton<IStringLocalizerFactory>(sp => sp.GetRequiredService<EFStringLocalizerFactory>());
            services.AddSingleton<IExtendedStringLocalizerFactory>(sp => sp.GetRequiredService<EFStringLocalizerFactory>());

            services.AddSingleton<ITranslationsPortabilityService, TranslationsPortabilityService>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITranslationsFormat, CsvTranslationsFormat>());

            return services;
        }
    }
}
