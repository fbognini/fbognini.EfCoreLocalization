using fbognini.EfCoreLocalization.Portability;
using Shouldly;
using Xunit;

namespace fbognini.EfCoreLocalization.Tests;

/// <summary>
/// Language.Id is nchar(5), so SQL Server returns short codes padded with trailing spaces. Sqlite does not pad, so the padding is seeded by hand.
/// </summary>
public class PaddedLanguageIdTests
{
    private static LocalizationTestHost NewHost()
    {
        return new LocalizationTestHost()
            .SeedLanguage("it   ", isDefault: true)
            .SeedLanguage("en   ");
    }

    [Fact]
    public void TheExportTrimsTheLanguageIds()
    {
        using var host = NewHost().SeedText("dashboard", "Home.Title", "Titolo", ("it   ", "Benvenuto"));

        var snapshot = host.Repository.ExportTranslations();

        snapshot.LanguageIds.ShouldBe(new[] { "it", "en" });
        snapshot.Rows.Single().Destinations["it"].ShouldBe("Benvenuto");
    }

    [Fact]
    public void ATrimmedColumn_MatchesThePaddedLanguage()
    {
        using var host = NewHost().SeedText("dashboard", "Home.Title", "Titolo", ("it   ", "Benvenuto"));
        var snapshot = new TranslationsSnapshot { LanguageIds = { "it" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Title",
            Destinations = { ["it"] = "Ciao" }
        });

        var result = host.Repository.ImportTranslations(snapshot);

        result.TranslationsUpdated.ShouldBe(1);
        result.TranslationsAdded.ShouldBe(0);
        host.Translations().Single().Destination.ShouldBe("Ciao");
    }

    [Fact]
    public void ACreatedTranslation_ReusesTheStoredLanguageId()
    {
        using var host = NewHost();
        var snapshot = new TranslationsSnapshot { LanguageIds = { "it" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Title",
            Destinations = { ["it"] = "Benvenuto" }
        });

        host.Repository.ImportTranslations(snapshot);

        host.Translations().Single().LanguageId.ShouldBe("it   ");
    }

    [Fact]
    public void ExportThenImport_ReportsNoChangeOnPaddedData()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it   ", "Benvenuto"), ("en   ", "Welcome"));

        var result = host.Repository.ImportTranslations(host.Repository.ExportTranslations());

        result.HasChanges.ShouldBeFalse();
        result.TranslationsUnchanged.ShouldBe(2);
    }
}
