using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Http;

namespace fbognini.EfCoreLocalization.Dashboard.Helpers;

internal static class FullSearchHelper
{
    public static async Task<FullSearchQueryParameters?> BindFromQueryAsync(HttpContext context)
    {
        var parameterInfo = typeof(FullSearchHelper).GetMethod(nameof(DummyMethod))!
            .GetParameters().First();

        return await FullSearchQueryParameters.BindAsync(context, parameterInfo);
    }

    // Exists only to hand BindAsync a ParameterInfo, which minimal APIs would otherwise supply themselves.
    public static void DummyMethod(FullSearchQueryParameters _) { }
}
