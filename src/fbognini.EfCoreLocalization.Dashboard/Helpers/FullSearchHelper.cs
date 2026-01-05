using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Http;

namespace fbognini.EfCoreLocalization.Dashboard.Helpers;

internal static class FullSearchHelper
{
    public static async Task<FullSearchQueryParameters?> BindFromQueryAsync(HttpContext context)
    {
        // Create a dummy ParameterInfo for BindAsync
        var parameterInfo = typeof(FullSearchHelper).GetMethod(nameof(DummyMethod))!
            .GetParameters().First();
        
        return await FullSearchQueryParameters.BindAsync(context, parameterInfo);
    }

    // Dummy method to get ParameterInfo
    public static void DummyMethod(FullSearchQueryParameters _) { }
}
