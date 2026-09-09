using fbognini.EfCoreLocalization.Portability;
using Shouldly;
using Xunit;

namespace fbognini.EfCoreLocalization.Tests;

public class ExportTranslationsTests
{
    private static LocalizationTestHost NewHost()
    {
        return new LocalizationTestHost()
            .SeedLanguage("en-US")
            .SeedLanguage("it-IT", isDefault: true)
            .SeedLanguage("de-DE", isActive: false);
    }

    [Fact]
    public void TheDefaultLanguageComesFirst_ThenTheOthersAlphabetically()
    {
        using var host = NewHost().SeedLanguage("fr-FR");

        var snapshot = host.Repository.ExportTranslations();

        snapshot.LanguageIds.ShouldBe(new[] { "it-IT", "en-US", "fr-FR" });
    }

    [Fact]
    public void InactiveLanguagesAreExcludedByDefault()
    {
        using var host = NewHost();

        host.Repository.ExportTranslations().LanguageIds.ShouldNotContain("de-DE");
        host.Repository.ExportTranslations(new TranslationsExportFilter { ActiveLanguagesOnly = false }).LanguageIds.ShouldContain("de-DE");
    }

    [Fact]
    public void RowsCarryTheTranslationsAndAreOrderedByKey()
    {
        using var host = NewHost()
            .SeedText("emails", "Welcome.Subject", "Oggetto", ("it-IT", "Benvenuto"))
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"), ("en-US", "Welcome"))
            .SeedText("dashboard", "Home.Body", "Corpo", ("it-IT", "Corpo"));

        var snapshot = host.Repository.ExportTranslations();

        snapshot.Rows.Select(x => x.TextId).ShouldBe(new[] { "Home.Body", "Home.Title", "Welcome.Subject" });
        snapshot.Rows.Select(x => x.ResourceId).ShouldBe(new[] { "dashboard", "dashboard", "emails" });

        var title = snapshot.Rows.Single(x => x.TextId == "Home.Title");
        title.Description.ShouldBe("Titolo");
        title.Destinations["it-IT"].ShouldBe("Benvenuto");
        title.Destinations["en-US"].ShouldBe("Welcome");

        var body = snapshot.Rows.Single(x => x.TextId == "Home.Body");
        (body.Destinations.TryGetValue("en-US", out var missing) && missing != null).ShouldBeFalse();
    }

    [Fact]
    public void AnEmptyDescription_IsExportedAsAnEmptyCell()
    {
        using var host = NewHost().SeedText("dashboard", "Home.Title", string.Empty, ("it-IT", "Benvenuto"));

        host.Repository.ExportTranslations().Rows.Single().Description.ShouldBeNull();
    }

    [Fact]
    public void TheResourceIdFilter_LimitsTheRows()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"))
            .SeedText("emails", "Welcome.Subject", "Oggetto", ("it-IT", "Benvenuto"));

        var snapshot = host.Repository.ExportTranslations(new TranslationsExportFilter { ResourceIds = ["dashboard"] });

        snapshot.Rows.ShouldHaveSingleItem().ResourceId.ShouldBe("dashboard");
    }

    [Fact]
    public void TheLanguageFilter_LimitsTheColumns()
    {
        using var host = NewHost().SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"), ("en-US", "Welcome"));

        var snapshot = host.Repository.ExportTranslations(new TranslationsExportFilter { LanguageIds = ["en-US"] });

        snapshot.LanguageIds.ShouldBe(new[] { "en-US" });
        snapshot.Rows.Single().Destinations.ContainsKey("it-IT").ShouldBeFalse();
    }

    [Fact]
    public void TextsWithoutTranslations_CanBeExcluded()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"))
            .SeedText("dashboard", "Home.Empty", "Vuoto");

        host.Repository.ExportTranslations().Rows.Count.ShouldBe(2);

        var snapshot = host.Repository.ExportTranslations(new TranslationsExportFilter { IncludeTextsWithoutTranslations = false });

        snapshot.Rows.ShouldHaveSingleItem().TextId.ShouldBe("Home.Title");
    }

    [Fact]
    public void TheExportStampsExportedOnUtc_AndTracksNothing()
    {
        using var host = NewHost().SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));

        var snapshot = host.Repository.ExportTranslations();

        snapshot.ExportedOnUtc.ShouldNotBeNull();
        host.Context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public void ExportThenImport_ReportsNoChange()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto; \"citato\""), ("en-US", "Welcome"))
            .SeedText("dashboard", "Home.Body", "Corpo\nsu due righe", ("it-IT", "Città\naccentata"))
            .SeedText("emails", "Welcome.Subject", "Oggetto", ("en-US", "Hello"));

        var exported = host.Repository.ExportTranslations();

        using var stream = new MemoryStream();
        var format = new CsvTranslationsFormat();
        format.Write(exported, stream);
        stream.Position = 0;

        var result = host.Repository.ImportTranslations(format.Read(stream));

        result.HasErrors.ShouldBeFalse();
        result.HasChanges.ShouldBeFalse();
        result.TranslationsUnchanged.ShouldBe(4);
    }
}
