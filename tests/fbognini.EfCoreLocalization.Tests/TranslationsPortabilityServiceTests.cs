using System.Text;
using fbognini.EfCoreLocalization.Excel;
using fbognini.EfCoreLocalization.Localizers;
using fbognini.EfCoreLocalization.Portability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Shouldly;
using Xunit;

namespace fbognini.EfCoreLocalization.Tests;

public class TranslationsPortabilityServiceTests
{
    private sealed class RecordingLocalizerFactory : IExtendedStringLocalizerFactory
    {
        public int ResetCount { get; private set; }

        public void ResetCache() => ResetCount++;

        public void ResetCache(Type resourceSource) => ResetCount++;

        public void ResetCache(string baseName, string location) => ResetCount++;

        public IStringLocalizer Create(Type resourceSource) => throw new NotSupportedException();

        public IStringLocalizer Create(string baseName, string location) => throw new NotSupportedException();
    }

    private static LocalizationTestHost NewHost()
    {
        return new LocalizationTestHost()
            .SeedLanguage("it-IT", isDefault: true)
            .SeedLanguage("en-US");
    }

    private static (TranslationsPortabilityService Service, RecordingLocalizerFactory Factory) NewService(LocalizationTestHost host)
    {
        var factory = new RecordingLocalizerFactory();
        return (new TranslationsPortabilityService(host.Repository, factory, [new CsvTranslationsFormat()]), factory);
    }

    private static MemoryStream Csv(params string[] lines)
    {
        return new MemoryStream(new UTF8Encoding(false).GetBytes(string.Join("\r\n", lines) + "\r\n"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("csv")]
    [InlineData("CSV")]
    [InlineData(".csv")]
    [InlineData("translations-20260909.csv")]
    public void ResolveFormat_AcceptsNamesExtensionsAndFileNames(string? candidate)
    {
        using var host = NewHost();
        var (service, _) = NewService(host);

        service.ResolveFormat(candidate).Name.ShouldBe("csv");
    }

    [Theory]
    [InlineData("xlsx")]
    [InlineData(".xlsx")]
    [InlineData("excel")]
    [InlineData("EXCEL")]
    [InlineData("translations-20260909.xlsx")]
    public void ResolveFormat_ResolvesTheExcelPackageByNameAndByAlias(string candidate)
    {
        using var host = NewHost();
        var service = new TranslationsPortabilityService(
            host.Repository,
            new RecordingLocalizerFactory(),
            [new CsvTranslationsFormat(), new XlsxTranslationsFormat()]);

        service.ResolveFormat(candidate).Name.ShouldBe("xlsx");
    }

    [Fact]
    public void ResolveFormat_OnAnUnknownFormat_Throws()
    {
        using var host = NewHost();
        var (service, _) = NewService(host);

        var exception = Should.Throw<NotSupportedException>(() => service.ResolveFormat("xlsx"));

        exception.Message.ShouldContain("csv", Case.Sensitive);
    }

    [Fact]
    public void Export_WritesTheFile()
    {
        using var host = NewHost().SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));
        var (service, _) = NewService(host);

        using var stream = new MemoryStream();
        service.Export(stream);

        var content = Encoding.UTF8.GetString(stream.ToArray());
        content.ShouldContain("ResourceId;TextId;Description;it-IT;en-US", Case.Sensitive);
        content.ShouldContain("dashboard;Home.Title;Titolo;Benvenuto", Case.Sensitive);
    }

    [Fact]
    public void Import_WithChanges_ResetsTheLocalizerCache()
    {
        using var host = NewHost();
        var (service, factory) = NewService(host);

        using var stream = Csv("ResourceId;TextId;Description;it-IT", "dashboard;Home.Title;Titolo;Benvenuto");
        var result = service.Import(stream);

        result.HasChanges.ShouldBeTrue();
        factory.ResetCount.ShouldBe(1);
    }

    [Fact]
    public void Import_WithoutChanges_LeavesTheCacheAlone()
    {
        using var host = NewHost().SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));
        var (service, factory) = NewService(host);

        using var stream = Csv("ResourceId;TextId;Description;it-IT", "dashboard;Home.Title;Titolo;Benvenuto");
        var result = service.Import(stream);

        result.HasChanges.ShouldBeFalse();
        factory.ResetCount.ShouldBe(0);
    }

    [Fact]
    public void Import_InDryRun_LeavesTheCacheAlone()
    {
        using var host = NewHost();
        var (service, factory) = NewService(host);

        using var stream = Csv("ResourceId;TextId;Description;it-IT", "dashboard;Home.Title;Titolo;Benvenuto");
        var result = service.Import(stream, new ImportTranslationsOptions { DryRun = true });

        result.HasChanges.ShouldBeTrue();
        factory.ResetCount.ShouldBe(0);
        host.Texts().ShouldBeEmpty();
    }

    [Fact]
    public void TheServiceAndTheCsvFormatAreRegistered()
    {
        using var host = NewHost();

        var service = host.Services.GetRequiredService<ITranslationsPortabilityService>();

        service.Formats.ShouldHaveSingleItem().Name.ShouldBe("csv");
    }

    [Fact]
    public void BothLocalizerFactoryRegistrationsResolveToTheSameInstance()
    {
        using var host = NewHost();

        var factory = host.Services.GetRequiredService<IStringLocalizerFactory>();
        var extended = host.Services.GetRequiredService<IExtendedStringLocalizerFactory>();

        extended.ShouldBeSameAs(factory);
    }
}
