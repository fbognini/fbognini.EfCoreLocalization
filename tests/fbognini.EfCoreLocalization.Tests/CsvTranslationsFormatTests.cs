using System.Text;
using fbognini.EfCoreLocalization.Portability;
using Shouldly;
using Xunit;

namespace fbognini.EfCoreLocalization.Tests;

public class CsvTranslationsFormatTests
{
    private static TranslationsSnapshot Read(string content, Encoding? encoding = null)
    {
        using var stream = new MemoryStream((encoding ?? new UTF8Encoding(false)).GetBytes(content));
        return new CsvTranslationsFormat().Read(stream);
    }

    private static string Write(TranslationsSnapshot snapshot, CsvTranslationsFormatOptions? options = null)
    {
        using var stream = new MemoryStream();
        new CsvTranslationsFormat(options ?? new CsvTranslationsFormatOptions()).Write(snapshot, stream);
        return new UTF8Encoding(false).GetString(stream.ToArray().AsSpan(GetBomLength(stream.ToArray())).ToArray());
    }

    private static int GetBomLength(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
    }

    private static TranslationsSnapshot BuildSnapshot()
    {
        var snapshot = new TranslationsSnapshot
        {
            LanguageIds = { "it-IT", "en-US" },
            ExportedOnUtc = new DateTime(2026, 9, 9, 10, 30, 0, DateTimeKind.Utc)
        };

        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Title",
            Description = "Titolo della home",
            Destinations = { ["it-IT"] = "Benvenuto; è un piacere", ["en-US"] = "Welcome" }
        });

        snapshot.Rows.Add(new TranslationsSnapshotRow
        {
            ResourceId = "dashboard",
            TextId = "Home.Body",
            Destinations = { ["it-IT"] = "Prima riga\nSeconda \"riga\"", ["en-US"] = null }
        });

        return snapshot;
    }

    [Fact]
    public void RoundTrip_PreservesEverything()
    {
        var original = BuildSnapshot();

        var roundTripped = Read(Write(original));

        roundTripped.LanguageIds.ShouldBe(original.LanguageIds);
        roundTripped.ExportedOnUtc.ShouldBe(original.ExportedOnUtc);
        roundTripped.Rows.Count.ShouldBe(original.Rows.Count);

        for (var i = 0; i < original.Rows.Count; i++)
        {
            var expected = original.Rows[i];
            var actual = roundTripped.Rows[i];

            actual.ResourceId.ShouldBe(expected.ResourceId);
            actual.TextId.ShouldBe(expected.TextId);
            actual.Description.ShouldBe(expected.Description);

            foreach (var languageId in original.LanguageIds)
            {
                actual.Destinations[languageId].ShouldBe(expected.Destinations[languageId]);
            }
        }
    }

    [Fact]
    public void Write_EmitsUtf8Bom()
    {
        using var stream = new MemoryStream();
        new CsvTranslationsFormat().Write(BuildSnapshot(), stream);

        var bytes = stream.ToArray();
        GetBomLength(bytes).ShouldBe(3);
    }

    [Fact]
    public void Write_LeavesTheStreamOpen()
    {
        using var stream = new MemoryStream();
        new CsvTranslationsFormat().Write(BuildSnapshot(), stream);

        stream.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void Write_EmitsSeparatorHintAndExportedOnUtc()
    {
        var lines = Write(BuildSnapshot()).Split("\r\n");

        lines[0].ShouldBe("sep=;");
        lines[1].ShouldStartWith("#exportedOnUtc=2026-09-09T10:30:00", Case.Sensitive);
        lines[2].ShouldStartWith("ResourceId;TextId;Description;it-IT;en-US", Case.Sensitive);
    }

    [Fact]
    public void Write_WithoutSeparatorHint_StartsWithTheHeader()
    {
        var options = new CsvTranslationsFormatOptions { WriteSeparatorHint = false, Separator = ',' };
        var snapshot = BuildSnapshot();
        snapshot.ExportedOnUtc = null;

        var lines = Write(snapshot, options).Split("\r\n");

        lines[0].ShouldStartWith("ResourceId,TextId,Description,it-IT,en-US", Case.Sensitive);
    }

    [Theory]
    [InlineData("ResourceId,TextId,Description,it-IT\r\ndashboard,Home.Title,,Benvenuto\r\n")]
    [InlineData("ResourceId;TextId;Description;it-IT\r\ndashboard;Home.Title;;Benvenuto\r\n")]
    [InlineData("ResourceId\tTextId\tDescription\tit-IT\r\ndashboard\tHome.Title\t\tBenvenuto\r\n")]
    [InlineData("sep=;\r\nResourceId;TextId;Description;it-IT\r\ndashboard;Home.Title;;Benvenuto\r\n")]
    public void Read_DetectsTheSeparator(string content)
    {
        var snapshot = Read(content);

        snapshot.LanguageIds.ShouldBe(new[] { "it-IT" });
        var row = snapshot.Rows.ShouldHaveSingleItem();
        row.ResourceId.ShouldBe("dashboard");
        row.TextId.ShouldBe("Home.Title");
        row.Destinations["it-IT"].ShouldBe("Benvenuto");
    }

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    [InlineData("\r")]
    public void Read_HandlesEveryLineEnding(string lineEnding)
    {
        var content = string.Join(lineEnding, "ResourceId;TextId;Description;it-IT", "dashboard;Home.Title;;Benvenuto", string.Empty);

        var row = Read(content).Rows.ShouldHaveSingleItem();

        row.Destinations["it-IT"].ShouldBe("Benvenuto");
    }

    [Fact]
    public void Read_HonoursUtf8Bom()
    {
        var content = "ResourceId;TextId;Description;it-IT\r\ndashboard;Home.Title;;Città\r\n";

        var row = Read(content, new UTF8Encoding(true)).Rows.ShouldHaveSingleItem();

        row.Destinations["it-IT"].ShouldBe("Città");
    }

    [Fact]
    public void Read_HandlesQuotedFields()
    {
        var content = "ResourceId;TextId;Description;it-IT\r\ndashboard;Home.Title;;\"Uno; due \"\"tre\"\"\r\nquattro\"\r\n";

        var row = Read(content).Rows.ShouldHaveSingleItem();

        row.Destinations["it-IT"].ShouldBe("Uno; due \"tre\"\nquattro");
    }

    [Fact]
    public void Read_TreatsEmptyCellsAsNotSupplied()
    {
        var content = "ResourceId;TextId;Description;it-IT;en-US\r\ndashboard;Home.Title;;;Welcome\r\n";

        var row = Read(content).Rows.ShouldHaveSingleItem();

        row.Destinations["it-IT"].ShouldBeNull();
        row.Description.ShouldBeNull();
        row.Destinations["en-US"].ShouldBe("Welcome");
    }

    [Fact]
    public void Read_KeepsWhitespaceOnlyValues()
    {
        var content = "ResourceId;TextId;Description;it-IT\r\ndashboard;Home.Title;;\" \"\r\n";

        var row = Read(content).Rows.ShouldHaveSingleItem();

        row.Destinations["it-IT"].ShouldBe(" ");
    }

    [Fact]
    public void Read_TolueratesShortAndLongRecords()
    {
        var content = "ResourceId;TextId;Description;it-IT;en-US\r\ndashboard;Short\r\ndashboard;Long;;Ciao;Hello;extra\r\n";

        var snapshot = Read(content);

        snapshot.Rows.Count.ShouldBe(2);
        snapshot.Rows[0].Destinations["it-IT"].ShouldBeNull();
        snapshot.Rows[1].Destinations["it-IT"].ShouldBe("Ciao");
        snapshot.Rows[1].Destinations["en-US"].ShouldBe("Hello");
    }

    [Fact]
    public void Read_SkipsBlankRecords()
    {
        var content = "ResourceId;TextId;Description;it-IT\r\ndashboard;A;;Uno\r\n\r\ndashboard;B;;Due\r\n\r\n";

        var snapshot = Read(content);

        snapshot.Rows.Count.ShouldBe(2);
        snapshot.Rows.Select(x => x.TextId).ShouldBe(new[] { "A", "B" });
    }

    [Fact]
    public void Read_NumbersSourceRowsIncludingPreambleAndHeader()
    {
        var content = "sep=;\r\n#exportedOnUtc=2026-09-09T10:30:00.0000000Z\r\nResourceId;TextId;Description;it-IT\r\ndashboard;A;;Uno\r\ndashboard;B;;Due\r\n";

        var snapshot = Read(content);

        snapshot.Rows[0].SourceRow.ShouldBe(4);
        snapshot.Rows[1].SourceRow.ShouldBe(5);
    }

    [Fact]
    public void Read_CountsAMultiLineFieldAsASingleRecord()
    {
        var content = "ResourceId;TextId;Description;it-IT\r\ndashboard;A;;\"Prima\r\nSeconda\"\r\ndashboard;B;;Due\r\n";

        var snapshot = Read(content);

        snapshot.Rows[0].SourceRow.ShouldBe(2);
        snapshot.Rows[1].SourceRow.ShouldBe(3);
    }

    [Fact]
    public void Read_NormalizesNewlinesInsideCells()
    {
        var content = "ResourceId;TextId;Description;it-IT\r\ndashboard;A;;\"Prima\r\nSeconda\rTerza\"\r\n";

        var row = Read(content).Rows.ShouldHaveSingleItem();

        row.Destinations["it-IT"].ShouldBe("Prima\nSeconda\nTerza");
    }

    [Fact]
    public void Read_AcceptsAHeaderWithoutDescription()
    {
        var content = "ResourceId;TextId;it-IT\r\ndashboard;A;Uno\r\n";

        var snapshot = Read(content);

        snapshot.LanguageIds.ShouldBe(new[] { "it-IT" });
        snapshot.Rows[0].Description.ShouldBeNull();
        snapshot.Rows[0].Destinations["it-IT"].ShouldBe("Uno");
    }

    [Fact]
    public void Read_MatchesTheHeaderCaseInsensitively()
    {
        var content = "resourceid;textid;description;it-IT\r\ndashboard;A;Desc;Uno\r\n";

        var row = Read(content).Rows.ShouldHaveSingleItem();

        row.ResourceId.ShouldBe("dashboard");
        row.Description.ShouldBe("Desc");
    }

    [Fact]
    public void Read_TrimsStructuralFieldsOnly()
    {
        var content = "ResourceId;TextId;Description; it-IT \r\n dashboard ; A ;;\" Uno \"\r\n";

        var snapshot = Read(content);

        snapshot.LanguageIds.ShouldBe(new[] { "it-IT" });
        snapshot.Rows[0].ResourceId.ShouldBe("dashboard");
        snapshot.Rows[0].TextId.ShouldBe("A");
        snapshot.Rows[0].Destinations["it-IT"].ShouldBe(" Uno ");
    }

    [Fact]
    public void Read_ThrowsWhenTheHeaderIsNotRecognizable()
    {
        Should.Throw<InvalidDataException>(() => Read("a;b;c\r\n1;2;3\r\n"));
    }

    [Fact]
    public void Read_OnAnEmptyStream_ReturnsAnEmptySnapshot()
    {
        var snapshot = Read(string.Empty);

        snapshot.LanguageIds.ShouldBeEmpty();
        snapshot.Rows.ShouldBeEmpty();
    }
}
