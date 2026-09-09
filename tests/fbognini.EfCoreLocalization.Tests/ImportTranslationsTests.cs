using fbognini.EfCoreLocalization.Portability;
using Shouldly;
using Xunit;

namespace fbognini.EfCoreLocalization.Tests;

public class ImportTranslationsTests
{
    private static readonly DateTime SeededOn = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static LocalizationTestHost NewHost()
    {
        return new LocalizationTestHost()
            .SeedLanguage("it-IT", isDefault: true)
            .SeedLanguage("en-US");
    }

    private static TranslationsSnapshot Snapshot(params TranslationsSnapshotRow[] rows)
    {
        var snapshot = new TranslationsSnapshot { LanguageIds = { "it-IT", "en-US" } };
        foreach (var row in rows)
        {
            snapshot.Rows.Add(row);
        }

        return snapshot;
    }

    private static TranslationsSnapshotRow Row(string textId, string? italian, string? english, string? description = null, string resourceId = "dashboard", int sourceRow = 0)
    {
        return new TranslationsSnapshotRow
        {
            ResourceId = resourceId,
            TextId = textId,
            Description = description,
            SourceRow = sourceRow,
            Destinations = { ["it-IT"] = italian, ["en-US"] = english }
        };
    }

    [Fact]
    public void OnAnEmptyDatabase_EverythingIsCreated()
    {
        using var host = NewHost();

        var result = host.Repository.ImportTranslations(Snapshot(
            Row("Home.Title", "Benvenuto", "Welcome", "Titolo"),
            Row("Home.Body", "Corpo", "Body")));

        result.HasErrors.ShouldBeFalse();
        result.TextsCreated.ShouldBe(2);
        result.TranslationsAdded.ShouldBe(4);
        result.TranslationsUpdated.ShouldBe(0);
        result.TranslationsUnchanged.ShouldBe(0);
        result.ResourceIds.ShouldBe(new[] { "dashboard" });
        result.LanguageIds.ShouldBe(new[] { "it-IT", "en-US" });

        host.Translations().Count.ShouldBe(4);
        host.Texts().Single(x => x.TextId == "Home.Title").Description.ShouldBe("Titolo");
    }

    [Fact]
    public void WithoutADescription_TheCreatedTextGetsAnEmptyOne()
    {
        using var host = NewHost();

        host.Repository.ImportTranslations(Snapshot(Row("Home.Title", "Benvenuto", "Welcome")));

        host.Texts().Single().Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void ReimportingTheSameSnapshot_ChangesNothing()
    {
        using var host = NewHost();
        var snapshot = Snapshot(Row("Home.Title", "Benvenuto", "Welcome", "Titolo"));
        host.Repository.ImportTranslations(snapshot);
        var before = host.Translations().Select(x => x.UpdatedOnUtc).ToList();

        var result = host.Repository.ImportTranslations(snapshot);

        result.HasChanges.ShouldBeFalse();
        result.TranslationsUnchanged.ShouldBe(2);
        result.TranslationsUpdated.ShouldBe(0);
        result.TextsUpdated.ShouldBe(0);
        host.Translations().Select(x => x.UpdatedOnUtc).ShouldBe(before);
    }

    [Fact]
    public void ASingleChangedCell_OnlyTouchesThatTranslation()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"), ("en-US", "Welcome"))
            .SeedText("dashboard", "Home.Body", "Corpo", ("it-IT", "Corpo"), ("en-US", "Body"));

        var result = host.Repository.ImportTranslations(Snapshot(
            Row("Home.Title", "Ciao", "Welcome"),
            Row("Home.Body", "Corpo", "Body")));

        result.TranslationsUpdated.ShouldBe(1);
        result.TranslationsUnchanged.ShouldBe(3);

        host.Translation("dashboard", "Home.Title", "it-IT")!.Destination.ShouldBe("Ciao");
        host.Translation("dashboard", "Home.Title", "it-IT")!.UpdatedOnUtc.ShouldBeGreaterThan(SeededOn);
        host.Translation("dashboard", "Home.Title", "en-US")!.UpdatedOnUtc.ShouldBe(SeededOn);
        host.Translation("dashboard", "Home.Body", "it-IT")!.UpdatedOnUtc.ShouldBe(SeededOn);
    }

    [Fact]
    public void AnEmptyCell_LeavesTheStoredValueUntouched()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"), ("en-US", "Welcome"));

        var result = host.Repository.ImportTranslations(Snapshot(Row("Home.Title", null, "Welcome")));

        result.TranslationsSkipped.ShouldBe(1);
        result.TranslationsUnchanged.ShouldBe(1);
        result.TranslationsUpdated.ShouldBe(0);
        host.Translation("dashboard", "Home.Title", "it-IT")!.Destination.ShouldBe("Benvenuto");
    }

    [Fact]
    public void AnEmptyDescription_LeavesTheStoredOneUntouched()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));

        var result = host.Repository.ImportTranslations(Snapshot(Row("Home.Title", "Benvenuto", null)));

        result.TextsUpdated.ShouldBe(0);
        host.Texts().Single().Description.ShouldBe("Titolo");
    }

    [Fact]
    public void ADifferentDescription_UpdatesTheText()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));

        var result = host.Repository.ImportTranslations(Snapshot(Row("Home.Title", "Benvenuto", null, "Titolo della home")));

        result.TextsUpdated.ShouldBe(1);
        host.Texts().Single().Description.ShouldBe("Titolo della home");
    }

    [Fact]
    public void DeleteNotMatched_OnlyTouchesTheResourceIdsInTheSnapshot()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"))
            .SeedText("dashboard", "Home.Gone", "Obsoleto", ("it-IT", "Vecchio"), ("en-US", "Old"))
            .SeedText("emails", "Welcome.Subject", "Oggetto", ("it-IT", "Benvenuto"));

        var result = host.Repository.ImportTranslations(
            Snapshot(Row("Home.Title", "Benvenuto", null)),
            new ImportTranslationsOptions { DeleteNotMatched = true });

        result.TextsDeleted.ShouldBe(1);
        result.TranslationsDeleted.ShouldBe(2);

        var texts = host.Texts();
        texts.Count.ShouldBe(2);
        texts.ShouldContain(x => x.ResourceId == "emails");
        texts.ShouldNotContain(x => x.TextId == "Home.Gone");
        host.Translations().Count(x => x.ResourceId == "emails").ShouldBe(1);
    }

    [Fact]
    public void DeleteNotMatched_KeepsKeysWhoseRowWasRejected()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"))
            .SeedText("dashboard", "Home.Other", "Altro", ("it-IT", "Altro"));

        var tooLong = new string('x', 101);
        var result = host.Repository.ImportTranslations(
            Snapshot(Row("Home.Title", "Benvenuto", null), Row("Home.Other", "Altro", null), Row(tooLong, "Nuovo", null)),
            new ImportTranslationsOptions { DeleteNotMatched = true });

        result.TextsDeleted.ShouldBe(0);
        host.Texts().Count.ShouldBe(2);
    }

    [Fact]
    public void ACaseMismatchOnTheKey_IsReportedInsteadOfInserted()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));

        var result = host.Repository.ImportTranslations(Snapshot(Row("home.title", "Ciao", null)));

        var error = result.Errors.ShouldHaveSingleItem();
        error.Kind.ShouldBe(ImportErrorKind.KeyCaseMismatch);
        error.Reason.ShouldContain("Home.Title", Case.Sensitive);
        result.TextsCreated.ShouldBe(0);
        host.Texts().ShouldHaveSingleItem();
        host.Translation("dashboard", "Home.Title", "it-IT")!.Destination.ShouldBe("Benvenuto");
    }

    [Fact]
    public void ARejectedRow_DoesNotStopTheOthers()
    {
        using var host = NewHost();
        var tooLong = new string('x', 101);

        var result = host.Repository.ImportTranslations(
            Snapshot(
                Row(tooLong, "Troppo lungo", null, sourceRow: 2),
                Row("Home.Title", "Benvenuto", null, sourceRow: 3),
                Row("Blocked", "Vietato", null, resourceId: "emails", sourceRow: 4)),
            new ImportTranslationsOptions { AllowedResourceIds = ["dashboard"] });

        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldContain(x => x.Kind == ImportErrorKind.ValueTooLong && x.SourceRow == 2);
        result.Errors.ShouldContain(x => x.Kind == ImportErrorKind.ResourceNotAllowed && x.SourceRow == 4);
        result.TextsCreated.ShouldBe(1);
        host.Texts().Single().TextId.ShouldBe("Home.Title");
    }

    [Fact]
    public void ADuplicatedRow_IsReportedOnce()
    {
        using var host = NewHost();

        var result = host.Repository.ImportTranslations(Snapshot(
            Row("Home.Title", "Benvenuto", null, sourceRow: 2),
            Row("Home.Title", "Ciao", null, sourceRow: 3)));

        var error = result.Errors.ShouldHaveSingleItem();
        error.Kind.ShouldBe(ImportErrorKind.DuplicateRow);
        error.SourceRow.ShouldBe(3);
        host.Translation("dashboard", "Home.Title", "it-IT")!.Destination.ShouldBe("Benvenuto");
    }

    [Fact]
    public void ATooLongDescription_IsReportedButTheRowIsStillImported()
    {
        using var host = NewHost();

        var result = host.Repository.ImportTranslations(Snapshot(Row("Home.Title", "Benvenuto", null, new string('x', 501))));

        result.Errors.ShouldHaveSingleItem().Kind.ShouldBe(ImportErrorKind.ValueTooLong);
        result.TextsCreated.ShouldBe(1);
        result.TranslationsAdded.ShouldBe(1);
        host.Texts().Single().Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void AnUnknownLanguageColumn_IsIgnoredWhileTheOthersAreImported()
    {
        using var host = NewHost();
        var snapshot = new TranslationsSnapshot { LanguageIds = { "it-IT", "zz-ZZ" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Title",
            Destinations = { ["it-IT"] = "Benvenuto", ["zz-ZZ"] = "Ignorato" }
        });

        var result = host.Repository.ImportTranslations(snapshot);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Kind.ShouldBe(ImportErrorKind.UnknownLanguage);
        error.LanguageId.ShouldBe("zz-ZZ");
        result.TranslationsAdded.ShouldBe(1);
        host.Translations().ShouldHaveSingleItem();
    }

    [Fact]
    public void WhenNoLanguageColumnResolves_ItThrowsWithTheUnknownCodes()
    {
        using var host = NewHost();
        var snapshot = new TranslationsSnapshot { LanguageIds = { "zz-ZZ" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow { ResourceId = "dashboard", TextId = "Home.Title" });

        var exception = Should.Throw<ArgumentException>(() => host.Repository.ImportTranslations(snapshot));

        exception.Message.ShouldContain("zz-ZZ", Case.Sensitive);
    }

    [Fact]
    public void ATwoLetterColumn_ResolvesToTheFullLanguage()
    {
        using var host = NewHost();
        var snapshot = new TranslationsSnapshot { LanguageIds = { "it" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Title",
            Destinations = { ["it"] = "Benvenuto" }
        });

        var result = host.Repository.ImportTranslations(snapshot);

        result.HasErrors.ShouldBeFalse();
        host.Translations().Single().LanguageId.Trim().ShouldBe("it-IT");
    }

    [Fact]
    public void WithCreateMissingTextsDisabled_UnknownKeysAreReported()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));

        var result = host.Repository.ImportTranslations(
            Snapshot(Row("Home.Title", "Ciao", null), Row("Home.New", "Nuovo", null)),
            new ImportTranslationsOptions { CreateMissingTexts = false });

        result.Errors.ShouldHaveSingleItem().Kind.ShouldBe(ImportErrorKind.TextNotFound);
        result.TextsCreated.ShouldBe(0);
        result.TranslationsUpdated.ShouldBe(1);
    }

    [Fact]
    public void WithCreateMissingTranslationsDisabled_MissingCellsAreReported()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"));

        var result = host.Repository.ImportTranslations(
            Snapshot(Row("Home.Title", "Ciao", "Welcome")),
            new ImportTranslationsOptions { CreateMissingTranslations = false });

        result.Errors.ShouldHaveSingleItem().Kind.ShouldBe(ImportErrorKind.TranslationNotFound);
        result.TranslationsAdded.ShouldBe(0);
        result.TranslationsUpdated.ShouldBe(1);
    }

    [Fact]
    public void DryRun_ReportsTheCountersWithoutWriting()
    {
        using var host = NewHost()
            .SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"))
            .SeedText("dashboard", "Home.Gone", "Obsoleto", ("it-IT", "Vecchio"));

        var result = host.Repository.ImportTranslations(
            Snapshot(Row("Home.Title", "Ciao", "Welcome"), Row("Home.New", "Nuovo", null)),
            new ImportTranslationsOptions { DryRun = true, DeleteNotMatched = true });

        result.DryRun.ShouldBeTrue();
        result.TextsCreated.ShouldBe(1);
        result.TranslationsUpdated.ShouldBe(1);
        result.TranslationsAdded.ShouldBe(2);
        result.TextsDeleted.ShouldBe(1);

        host.Texts().Count.ShouldBe(2);
        host.Translation("dashboard", "Home.Title", "it-IT")!.Destination.ShouldBe("Benvenuto");
        host.Texts().ShouldContain(x => x.TextId == "Home.Gone");
    }

    [Fact]
    public void AfterAnImport_TheChangeTrackerIsEmpty()
    {
        using var host = NewHost();

        host.Repository.ImportTranslations(Snapshot(Row("Home.Title", "Benvenuto", "Welcome")));

        host.Context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public void AfterAFailedImport_TheChangeTrackerIsEmpty()
    {
        using var host = NewHost();
        var snapshot = new TranslationsSnapshot { LanguageIds = { "zz-ZZ" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow { ResourceId = "dashboard", TextId = "Home.Title" });

        Should.Throw<ArgumentException>(() => host.Repository.ImportTranslations(snapshot));

        host.Context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public void ARowWithoutAKey_IsReported()
    {
        using var host = NewHost();

        var result = host.Repository.ImportTranslations(Snapshot(Row(string.Empty, "Benvenuto", null, sourceRow: 2)));

        result.Errors.ShouldHaveSingleItem().Kind.ShouldBe(ImportErrorKind.MissingKey);
        host.Texts().ShouldBeEmpty();
    }
}
