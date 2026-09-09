using fbognini.EfCoreLocalization.Portability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace fbognini.EfCoreLocalization.Excel;

public static class EfCoreLocalizationExcelServiceCollectionExtensions
{
    public static IServiceCollection AddEfCoreLocalizationExcel(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITranslationsFormat, XlsxTranslationsFormat>());

        return services;
    }
}
