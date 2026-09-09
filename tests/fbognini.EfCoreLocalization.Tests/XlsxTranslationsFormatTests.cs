using ClosedXML.Excel;
using fbognini.EfCoreLocalization.Excel;
using fbognini.EfCoreLocalization.Portability;
using Shouldly;
using Xunit;

namespace fbognini.EfCoreLocalization.Tests;

public class XlsxTranslationsFormatTests
{
    private static TranslationsSnapshot RoundTrip(TranslationsSnapshot snapshot)
    {
        var format = new XlsxTranslationsFormat();

        using var stream = new MemoryStream();
        format.Write(snapshot, stream);
        stream.Position = 0;

        return format.Read(stream);
    }

    private static TranslationsSnapshot BuildSnapshot()
    {
        var snapshot = new TranslationsSnapshot
        {
            LanguageIds = { "it-IT", "en-US", "de-DE" },
            ExportedOnUtc = new DateTime(2026, 9, 9, 10, 30, 0, DateTimeKind.Utc)
        };

        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Title",
            Description = "Titolo",
            Destinations = { ["it-IT"] = "Benvenuto", ["en-US"] = "Welcome", ["de-DE"] = "Willkommen" }
        });

        // The row that breaks non positional reading: without it French would slide into the German column.
        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Body",
            Destinations = { ["it-IT"] = null, ["en-US"] = null, ["de-DE"] = "Körper" }
        });

        return snapshot;
    }

    [Fact]
    public void RoundTrip_PreservesEverything()
    {
        var original = BuildSnapshot();

        var roundTripped = RoundTrip(original);

        roundTripped.LanguageIds.ShouldBe(original.LanguageIds);
        roundTripped.ExportedOnUtc.ShouldBe(original.ExportedOnUtc);
        roundTripped.Rows.Count.ShouldBe(2);

        var title = roundTripped.Rows[0];
        title.TextId.ShouldBe("Home.Title");
        title.Description.ShouldBe("Titolo");
        title.Destinations["it-IT"].ShouldBe("Benvenuto");
        title.Destinations["en-US"].ShouldBe("Welcome");
        title.Destinations["de-DE"].ShouldBe("Willkommen");
    }

    [Fact]
    public void EmptyCellsInTheMiddle_DoNotShiftTheColumns()
    {
        var roundTripped = RoundTrip(BuildSnapshot());

        var body = roundTripped.Rows[1];
        body.Destinations["it-IT"].ShouldBeNull();
        body.Destinations["en-US"].ShouldBeNull();
        body.Destinations["de-DE"].ShouldBe("Körper");
        body.Description.ShouldBeNull();
    }

    [Fact]
    public void AValueStartingWithAnEqualSign_StaysText()
    {
        var snapshot = new TranslationsSnapshot { LanguageIds = { "it-IT" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Formula",
            Destinations = { ["it-IT"] = "=1+1" }
        });

        using var stream = new MemoryStream();
        new XlsxTranslationsFormat().Write(snapshot, stream);
        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        var cell = workbook.Worksheet("translations").Cell(2, 4);
        cell.HasFormula.ShouldBeFalse();
        cell.GetString().ShouldBe("=1+1");
    }

    [Fact]
    public void MultiLineValues_AreNormalized()
    {
        var snapshot = new TranslationsSnapshot { LanguageIds = { "it-IT" } };
        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Multi",
            Destinations = { ["it-IT"] = "Prima\nSeconda" }
        });

        RoundTrip(snapshot).Rows.Single().Destinations["it-IT"].ShouldBe("Prima\nSeconda");
    }

    [Fact]
    public void TheHeaderIsFrozenAndBold()
    {
        using var stream = new MemoryStream();
        new XlsxTranslationsFormat().Write(BuildSnapshot(), stream);
        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("translations");

        sheet.Row(1).Style.Font.Bold.ShouldBeTrue();
        sheet.SheetView.SplitRow.ShouldBe(1);
    }

    [Fact]
    public void TheMetadataSheetIsHidden()
    {
        using var stream = new MemoryStream();
        new XlsxTranslationsFormat().Write(BuildSnapshot(), stream);
        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);

        workbook.Worksheet("_meta").Visibility.ShouldBe(XLWorksheetVisibility.Hidden);
    }

    [Fact]
    public void Write_LeavesTheStreamOpen()
    {
        using var stream = new MemoryStream();
        new XlsxTranslationsFormat().Write(BuildSnapshot(), stream);

        stream.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void Read_AcceptsANonSeekableStream()
    {
        using var buffer = new MemoryStream();
        new XlsxTranslationsFormat().Write(BuildSnapshot(), buffer);

        using var forwardOnly = new ForwardOnlyStream(buffer.ToArray());
        var snapshot = new XlsxTranslationsFormat().Read(forwardOnly);

        snapshot.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public void Read_ThrowsWhenTheHeaderIsNotRecognizable()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("translations");
        sheet.Cell(1, 1).SetValue("a");
        sheet.Cell(1, 2).SetValue("b");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        Should.Throw<InvalidDataException>(() => new XlsxTranslationsFormat().Read(stream));
    }

    [Fact]
    public void Read_NumbersSourceRowsAsSheetRows()
    {
        var roundTripped = RoundTrip(BuildSnapshot());

        roundTripped.Rows[0].SourceRow.ShouldBe(2);
        roundTripped.Rows[1].SourceRow.ShouldBe(3);
    }

    [Fact]
    public void ExportThenImport_ReportsNoChange()
    {
        using var host = new LocalizationTestHost()
            .SeedLanguage("it-IT", isDefault: true)
            .SeedLanguage("en-US");
        host.SeedText("dashboard", "Home.Title", "Titolo", ("it-IT", "Benvenuto"), ("en-US", "Welcome"));
        host.SeedText("dashboard", "Home.Body", "Corpo", ("it-IT", "Città\naccentata"));

        var format = new XlsxTranslationsFormat();
        using var stream = new MemoryStream();
        format.Write(host.Repository.ExportTranslations(), stream);
        stream.Position = 0;

        var result = host.Repository.ImportTranslations(format.Read(stream));

        result.HasErrors.ShouldBeFalse();
        result.HasChanges.ShouldBeFalse();
        result.TranslationsUnchanged.ShouldBe(3);
    }

    private sealed class ForwardOnlyStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
