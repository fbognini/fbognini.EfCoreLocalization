using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace fbognini.EfCoreLocalization.Dashboard.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEfCoreLocalizationDashboard(this IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddValidatorsFromAssemblyContaining<IMarker>();

            return services;
        }
    }
}
