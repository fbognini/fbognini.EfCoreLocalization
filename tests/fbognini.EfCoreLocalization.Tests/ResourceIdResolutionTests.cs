using fbognini.EfCoreLocalization.Localizers;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace fbognini.EfCoreLocalization.Tests;

public class ResourceIdResolutionTests
{
    private class HomeModel
    {
    }

    [LocalizationKey("dashboard")]
    private class TaggedModel
    {
    }

    private static EFStringLocalizerFactory NewFactory(LocalizationTestHost host, Action<EfCoreLocalizationSettings> configure)
    {
        var settings = new EfCoreLocalizationSettings();
        configure(settings);

        return new EFStringLocalizerFactory(host.Repository, Options.Create(settings));
    }

    [Fact]
    public void GlobalResourceId_ReplacesTheTypeName()
    {
        using var host = new LocalizationTestHost();
        var factory = NewFactory(host, x => x.GlobalResourceId = "global");

        factory.GetResourceIdFromType(typeof(HomeModel)).ShouldBe("global");
    }

    [Fact]
    public void GlobalResourceId_ReplacesTheLocalizationKey()
    {
        using var host = new LocalizationTestHost();
        var factory = NewFactory(host, x => x.GlobalResourceId = "global");

        factory.GetResourceIdFromType(typeof(TaggedModel)).ShouldBe("global");
    }

    [Fact]
    public void WithoutGlobalResourceId_TheLocalizationKeyIsTheResourceId()
    {
        using var host = new LocalizationTestHost();
        var factory = NewFactory(host, x => x.ResourceIdPrefix = "app");

        factory.GetResourceIdFromType(typeof(TaggedModel)).ShouldBe("dashboard");
    }

    [Fact]
    public void GlobalResourceId_ReplacesTheBaseNameAndTheLocation()
    {
        var resourceId = $"global-{Guid.NewGuid():N}";
        using var host = new LocalizationTestHost()
            .SeedLanguage("it", isDefault: true)
            .SeedText(resourceId, "Welcome", "", ("it", "Benvenuto"));
        var factory = NewFactory(host, x => x.GlobalResourceId = resourceId);

        var localizer = factory.Create("Index", "SampleWebApp.Pages");

        localizer.GetAllStrings(false).Select(x => x.Value).ShouldBe(["Benvenuto"]);
    }

    [Fact]
    public void WithoutGlobalResourceId_TheLocationIsPrependedToTheBaseName()
    {
        var location = $"Pages{Guid.NewGuid():N}";
        using var host = new LocalizationTestHost()
            .SeedLanguage("it", isDefault: true)
            .SeedText($"{location}.Index", "Welcome", "", ("it", "Benvenuto"));
        var factory = NewFactory(host, _ => { });

        var localizer = factory.Create("Index", location);

        localizer.GetAllStrings(false).Select(x => x.Value).ShouldBe(["Benvenuto"]);
    }
}
