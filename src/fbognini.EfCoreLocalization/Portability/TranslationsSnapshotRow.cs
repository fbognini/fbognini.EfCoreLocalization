using System.Collections.Generic;

namespace fbognini.EfCoreLocalization.Portability;

public class TranslationsSnapshotRow
{
    public string ResourceId { get; set; } = null!;

    public string TextId { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// LanguageId to text. A missing key or a null value means no value was supplied and the translation is left untouched.
    /// </summary>
    public IDictionary<string, string?> Destinations { get; set; } = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 1-based record number in the source file, preambles and header included. Only set when reading, 0 on export.
    /// </summary>
    public int SourceRow { get; set; }
}
