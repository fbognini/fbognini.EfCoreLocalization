using System;
using System.Collections.Generic;

namespace fbognini.EfCoreLocalization.Portability;

/// <summary>
/// Format-neutral, wide representation of a translation catalog: one row per key, one language per column.
/// </summary>
public class TranslationsSnapshot
{
    public IList<string> LanguageIds { get; set; } = new List<string>();

    public IList<TranslationsSnapshotRow> Rows { get; set; } = new List<TranslationsSnapshotRow>();

    /// <summary>
    /// Set by the export and carried inside the file. Reserved for a future concurrency check on import.
    /// </summary>
    public DateTime? ExportedOnUtc { get; set; }
}
