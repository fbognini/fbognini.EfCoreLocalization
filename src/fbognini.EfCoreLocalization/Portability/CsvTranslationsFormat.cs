using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace fbognini.EfCoreLocalization.Portability;

public class CsvTranslationsFormat : ITranslationsFormat
{
    private const string ResourceIdHeader = "ResourceId";
    private const string TextIdHeader = "TextId";
    private const string DescriptionHeader = "Description";
    private const string ExportedOnUtcKey = "exportedOnUtc";

    private static readonly char[] CandidateSeparators = [';', ',', '\t', '|'];

    private readonly CsvTranslationsFormatOptions _options;

    public CsvTranslationsFormat()
        : this(new CsvTranslationsFormatOptions())
    {
    }

    public CsvTranslationsFormat(CsvTranslationsFormatOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "csv";

    public string ContentType => "text/csv";

    public string FileExtension => ".csv";

    public void Write(TranslationsSnapshot snapshot, Stream stream)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var separator = _options.Separator;

        // leaveOpen, otherwise disposing the writer closes the caller stream, HttpResponse.Body included.
        using var writer = new StreamWriter(stream, _options.Encoding, 1024, leaveOpen: true)
        {
            NewLine = "\r\n"
        };

        if (_options.WriteSeparatorHint)
        {
            writer.WriteLine("sep=" + separator);
        }

        if (snapshot.ExportedOnUtc.HasValue)
        {
            writer.WriteLine("#" + ExportedOnUtcKey + "=" + snapshot.ExportedOnUtc.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        var languageIds = snapshot.LanguageIds ?? new List<string>();

        var header = new List<string>(3 + languageIds.Count) { ResourceIdHeader, TextIdHeader, DescriptionHeader };
        header.AddRange(languageIds);
        writer.WriteLine(BuildLine(header, separator));

        foreach (var row in snapshot.Rows ?? new List<TranslationsSnapshotRow>())
        {
            var fields = new List<string?>(header.Count) { row.ResourceId, row.TextId, row.Description };
            foreach (var languageId in languageIds)
            {
                fields.Add(row.Destinations != null && row.Destinations.TryGetValue(languageId, out var destination) ? destination : null);
            }

            writer.WriteLine(BuildLine(fields, separator));
        }

        writer.Flush();
    }

    public TranslationsSnapshot Read(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        string content;
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true))
        {
            content = reader.ReadToEnd();
        }

        var snapshot = new TranslationsSnapshot();

        var preambleLines = ReadPreamble(content, out var contentStart, out var declaredSeparator, out var exportedOnUtc);
        snapshot.ExportedOnUtc = exportedOnUtc;

        var body = content.Substring(contentStart);
        var separator = declaredSeparator ?? DetectSeparator(body);

        var records = ParseRecords(body, separator);
        if (records.Count == 0)
        {
            return snapshot;
        }

        var header = records[0];
        var resourceIdIndex = IndexOfHeader(header, ResourceIdHeader);
        var textIdIndex = IndexOfHeader(header, TextIdHeader);
        if (resourceIdIndex < 0 || textIdIndex < 0)
        {
            throw new InvalidDataException("The file does not look like a translations export: the header must contain both a " + ResourceIdHeader + " and a " + TextIdHeader + " column.");
        }

        var descriptionIndex = IndexOfHeader(header, DescriptionHeader);

        var languageColumns = new List<KeyValuePair<int, string>>();
        var seenLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
        {
            if (i == resourceIdIndex || i == textIdIndex || i == descriptionIndex)
            {
                continue;
            }

            var languageId = header[i].Trim();
            if (languageId.Length == 0 || !seenLanguages.Add(languageId))
            {
                continue;
            }

            languageColumns.Add(new KeyValuePair<int, string>(i, languageId));
            snapshot.LanguageIds.Add(languageId);
        }

        for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
        {
            var record = records[recordIndex];
            if (IsEmptyRecord(record))
            {
                continue;
            }

            var row = new TranslationsSnapshotRow
            {
                ResourceId = GetField(record, resourceIdIndex)?.Trim() ?? string.Empty,
                TextId = GetField(record, textIdIndex)?.Trim() ?? string.Empty,
                Description = NullIfEmpty(GetField(record, descriptionIndex)),
                SourceRow = preambleLines + recordIndex + 1
            };

            foreach (var column in languageColumns)
            {
                row.Destinations[column.Value] = NullIfEmpty(GetField(record, column.Key));
            }

            snapshot.Rows.Add(row);
        }

        return snapshot;
    }

    private static int ReadPreamble(string content, out int contentStart, out char? declaredSeparator, out DateTime? exportedOnUtc)
    {
        declaredSeparator = null;
        exportedOnUtc = null;

        var position = 0;
        var lines = 0;

        while (position < content.Length)
        {
            var lineEnd = content.IndexOfAny(['\r', '\n'], position);
            var line = lineEnd < 0 ? content.Substring(position) : content.Substring(position, lineEnd - position);

            if (line.Length == 5 && line.StartsWith("sep=", StringComparison.OrdinalIgnoreCase))
            {
                declaredSeparator = line[4];
            }
            else if (TryParseMetadataLine(line, out var key, out var value))
            {
                if (string.Equals(key, ExportedOnUtcKey, StringComparison.OrdinalIgnoreCase)
                    && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    exportedOnUtc = parsed.ToUniversalTime();
                }
            }
            else
            {
                break;
            }

            lines++;
            position = SkipLineBreak(content, lineEnd);
            if (lineEnd < 0)
            {
                break;
            }
        }

        contentStart = position;
        return lines;
    }

    private static bool TryParseMetadataLine(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        if (line.Length < 3 || line[0] != '#')
        {
            return false;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex < 2)
        {
            return false;
        }

        key = line.Substring(1, separatorIndex - 1);
        value = line.Substring(separatorIndex + 1);
        return true;
    }

    private static int SkipLineBreak(string content, int lineEnd)
    {
        if (lineEnd < 0)
        {
            return content.Length;
        }

        if (content[lineEnd] == '\r' && lineEnd + 1 < content.Length && content[lineEnd + 1] == '\n')
        {
            return lineEnd + 2;
        }

        return lineEnd + 1;
    }

    /// <summary>
    /// Picks the separator that makes the first record parse as a translations header, falling back to raw frequency outside quotes.
    /// </summary>
    private static char DetectSeparator(string body)
    {
        foreach (var candidate in CandidateSeparators)
        {
            var records = ParseRecords(body, candidate, stopAfterFirstRecord: true);
            if (records.Count == 0 || records[0].Count < 3)
            {
                continue;
            }

            if (IndexOfHeader(records[0], ResourceIdHeader) >= 0 && IndexOfHeader(records[0], TextIdHeader) >= 0)
            {
                return candidate;
            }
        }

        var best = ',';
        var bestCount = -1;
        foreach (var candidate in CandidateSeparators)
        {
            var count = CountOutsideQuotes(body, candidate);
            if (count > bestCount)
            {
                best = candidate;
                bestCount = count;
            }
        }

        return best;
    }

    private static int CountOutsideQuotes(string body, char separator)
    {
        var count = 0;
        var quoted = false;

        for (var i = 0; i < body.Length; i++)
        {
            var c = body[i];
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (quoted)
            {
                continue;
            }

            if (c == '\r' || c == '\n')
            {
                break;
            }

            if (c == separator)
            {
                count++;
            }
        }

        return count;
    }

    private static List<List<string>> ParseRecords(string content, char separator, bool stopAfterFirstRecord = false)
    {
        var records = new List<List<string>>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var started = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }

                    continue;
                }

                // Newlines inside a cell are normalized, otherwise every multi-line value would look modified on each round trip.
                if (c == '\r')
                {
                    if (i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }

                    field.Append('\n');
                    continue;
                }

                field.Append(c);
                continue;
            }

            if (c == '"' && field.Length == 0)
            {
                quoted = true;
                started = true;
                continue;
            }

            if (c == separator)
            {
                fields.Add(field.ToString());
                field.Clear();
                started = true;
                continue;
            }

            if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }

                fields.Add(field.ToString());
                field.Clear();
                records.Add(fields);
                fields = new List<string>();
                started = false;

                if (stopAfterFirstRecord)
                {
                    return records;
                }

                continue;
            }

            field.Append(c);
            started = true;
        }

        if (started || field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            records.Add(fields);
        }

        return records;
    }

    private static string BuildLine(IEnumerable<string?> fields, char separator)
    {
        var builder = new StringBuilder();
        var first = true;

        foreach (var field in fields)
        {
            if (!first)
            {
                builder.Append(separator);
            }

            builder.Append(Escape(field, separator));
            first = false;
        }

        return builder.ToString();
    }

    private static string Escape(string? value, char separator)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuoting = value!.IndexOf(separator) >= 0
            || value.IndexOf('"') >= 0
            || value.IndexOf('\n') >= 0
            || value.IndexOf('\r') >= 0
            || char.IsWhiteSpace(value[0])
            || char.IsWhiteSpace(value[value.Length - 1]);

        if (!needsQuoting)
        {
            return value;
        }

        return string.Concat("\"", value.Replace("\"", "\"\""), "\"");
    }

    private static int IndexOfHeader(List<string> header, string name)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? GetField(List<string> record, int index)
    {
        return index >= 0 && index < record.Count ? record[index] : null;
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool IsEmptyRecord(List<string> record)
    {
        foreach (var field in record)
        {
            if (!string.IsNullOrEmpty(field))
            {
                return false;
            }
        }

        return true;
    }
}
