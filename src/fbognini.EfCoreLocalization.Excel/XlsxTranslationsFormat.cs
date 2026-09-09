using ClosedXML.Excel;
using fbognini.EfCoreLocalization.Portability;
using System.Globalization;

namespace fbognini.EfCoreLocalization.Excel;

public class XlsxTranslationsFormat : ITranslationsFormat
{
    private const string TranslationsSheetName = "translations";
    private const string MetadataSheetName = "_meta";
    private const string ResourceIdHeader = "ResourceId";
    private const string TextIdHeader = "TextId";
    private const string DescriptionHeader = "Description";
    private const string ExportedOnUtcKey = "exportedOnUtc";

    public string Name => "xlsx";

    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string FileExtension => ".xlsx";

    // The package is called .Excel, so "excel" has to resolve too.
    public IReadOnlyCollection<string> Aliases => ["excel"];

    public void Write(TranslationsSnapshot snapshot, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(stream);

        var languageIds = snapshot.LanguageIds ?? new List<string>();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(TranslationsSheetName);

        var header = new List<string>(3 + languageIds.Count) { ResourceIdHeader, TextIdHeader, DescriptionHeader };
        header.AddRange(languageIds);

        for (var column = 0; column < header.Count; column++)
        {
            sheet.Cell(1, column + 1).SetValue(header[column]);
            sheet.Column(column + 1).Width = column switch
            {
                0 => 16,
                1 => 45,
                2 => 40,
                _ => 60
            };
        }

        // Text format on the language columns, so that a translation is never reinterpreted as a number or a date.
        for (var column = 4; column <= header.Count; column++)
        {
            sheet.Column(column).Style.NumberFormat.Format = "@";
        }

        var rowNumber = 2;
        foreach (var row in snapshot.Rows ?? new List<TranslationsSnapshotRow>())
        {
            sheet.Cell(rowNumber, 1).SetValue(row.ResourceId);
            sheet.Cell(rowNumber, 2).SetValue(row.TextId);
            sheet.Cell(rowNumber, 3).SetValue(row.Description ?? string.Empty);

            for (var i = 0; i < languageIds.Count; i++)
            {
                var destination = row.Destinations != null && row.Destinations.TryGetValue(languageIds[i], out var value) ? value : null;
                sheet.Cell(rowNumber, 4 + i).SetValue(destination ?? string.Empty);
            }

            rowNumber++;
        }

        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(rowNumber - 1, 1), header.Count).SetAutoFilter();

        if (snapshot.ExportedOnUtc.HasValue)
        {
            var metadata = workbook.Worksheets.Add(MetadataSheetName);
            metadata.Cell(1, 1).SetValue(ExportedOnUtcKey);
            metadata.Cell(1, 2).SetValue(snapshot.ExportedOnUtc.Value.ToString("O", CultureInfo.InvariantCulture));
            metadata.Hide();
        }

        // ClosedXML needs a seekable stream, and HttpResponse.Body is not one.
        using var buffer = new MemoryStream();
        workbook.SaveAs(buffer);
        buffer.Position = 0;
        buffer.CopyTo(stream);
        stream.Flush();
    }

    public TranslationsSnapshot Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var workbook = OpenWorkbook(stream);

        var snapshot = new TranslationsSnapshot { ExportedOnUtc = ReadExportedOnUtc(workbook) };

        var sheet = workbook.Worksheets.FirstOrDefault(x => string.Equals(x.Name, TranslationsSheetName, StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault(x => !string.Equals(x.Name, MetadataSheetName, StringComparison.OrdinalIgnoreCase));

        var range = sheet?.RangeUsed();
        if (range == null)
        {
            return snapshot;
        }

        var firstRow = range.FirstRow().RowNumber();
        var lastRow = range.LastRow().RowNumber();
        var firstColumn = range.FirstColumn().ColumnNumber();
        var lastColumn = range.LastColumn().ColumnNumber();

        var headerRow = sheet!.Row(firstRow);
        var resourceIdColumn = -1;
        var textIdColumn = -1;
        var descriptionColumn = -1;
        var languageColumns = new List<KeyValuePair<int, string>>();
        var seenLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            var name = headerRow.Cell(column).GetFormattedString().Trim();
            if (name.Length == 0)
            {
                continue;
            }

            if (resourceIdColumn < 0 && string.Equals(name, ResourceIdHeader, StringComparison.OrdinalIgnoreCase))
            {
                resourceIdColumn = column;
            }
            else if (textIdColumn < 0 && string.Equals(name, TextIdHeader, StringComparison.OrdinalIgnoreCase))
            {
                textIdColumn = column;
            }
            else if (descriptionColumn < 0 && string.Equals(name, DescriptionHeader, StringComparison.OrdinalIgnoreCase))
            {
                descriptionColumn = column;
            }
            else if (seenLanguages.Add(name))
            {
                languageColumns.Add(new KeyValuePair<int, string>(column, name));
                snapshot.LanguageIds.Add(name);
            }
        }

        if (resourceIdColumn < 0 || textIdColumn < 0)
        {
            throw new InvalidDataException("The workbook does not look like a translations export: the header must contain both a " + ResourceIdHeader + " and a " + TextIdHeader + " column.");
        }

        for (var rowNumber = firstRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            // Positional access: IXLRow.Cells() skips empty cells, which would slide every value one column to the left.
            var sheetRow = sheet.Row(rowNumber);
            if (sheetRow.IsEmpty())
            {
                continue;
            }

            var row = new TranslationsSnapshotRow
            {
                ResourceId = GetValue(sheetRow, resourceIdColumn)?.Trim() ?? string.Empty,
                TextId = GetValue(sheetRow, textIdColumn)?.Trim() ?? string.Empty,
                Description = GetValue(sheetRow, descriptionColumn),
                SourceRow = rowNumber
            };

            foreach (var column in languageColumns)
            {
                row.Destinations[column.Value] = GetValue(sheetRow, column.Key);
            }

            snapshot.Rows.Add(row);
        }

        return snapshot;
    }

    private static XLWorkbook OpenWorkbook(Stream stream)
    {
        if (stream.CanSeek)
        {
            return new XLWorkbook(stream);
        }

        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;
        return new XLWorkbook(buffer);
    }

    private static DateTime? ReadExportedOnUtc(XLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet(MetadataSheetName, out var metadata))
        {
            return null;
        }

        var range = metadata.RangeUsed();
        if (range == null)
        {
            return null;
        }

        for (var rowNumber = range.FirstRow().RowNumber(); rowNumber <= range.LastRow().RowNumber(); rowNumber++)
        {
            var key = metadata.Row(rowNumber).Cell(1).GetFormattedString().Trim();
            if (!string.Equals(key, ExportedOnUtcKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = metadata.Row(rowNumber).Cell(2).GetFormattedString().Trim();
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed.ToUniversalTime();
            }
        }

        return null;
    }

    private static string? GetValue(IXLRow row, int column)
    {
        if (column < 0)
        {
            return null;
        }

        var cell = row.Cell(column);
        if (cell.IsEmpty())
        {
            return null;
        }

        var value = cell.GetFormattedString();
        if (value.Length == 0)
        {
            return null;
        }

        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
