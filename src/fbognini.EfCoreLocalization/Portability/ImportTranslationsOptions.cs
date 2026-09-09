using System.Collections.Generic;

namespace fbognini.EfCoreLocalization.Portability;

public class ImportTranslationsOptions
{
    public bool CreateMissingTexts { get; set; } = true;

    public bool CreateMissingTranslations { get; set; } = true;

    /// <summary>
    /// Deletes keys that exist in the database but not in the snapshot, limited to the resource ids the snapshot contains.
    /// </summary>
    public bool DeleteNotMatched { get; set; }

    /// <summary>
    /// Runs the whole computation and returns the counters without writing anything.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Rows whose resource id is outside this list are rejected. Null means no constraint.
    /// </summary>
    public ICollection<string>? AllowedResourceIds { get; set; }
}
