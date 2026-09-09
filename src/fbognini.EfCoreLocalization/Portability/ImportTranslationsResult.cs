using System.Collections.Generic;

namespace fbognini.EfCoreLocalization.Portability;

public class ImportTranslationsResult
{
    public bool DryRun { get; set; }

    public int TotalRows { get; set; }

    public IList<string> ResourceIds { get; set; } = new List<string>();

    public IList<string> LanguageIds { get; set; } = new List<string>();

    public int TextsCreated { get; set; }

    public int TextsUpdated { get; set; }

    public int TextsDeleted { get; set; }

    public int TranslationsAdded { get; set; }

    public int TranslationsUpdated { get; set; }

    public int TranslationsUnchanged { get; set; }

    public int TranslationsDeleted { get; set; }

    public int TranslationsSkipped { get; set; }

    public IList<ImportTranslationsError> Errors { get; set; } = new List<ImportTranslationsError>();

    public bool HasErrors => Errors.Count > 0;

    public bool HasChanges => TextsCreated + TextsUpdated + TextsDeleted + TranslationsAdded + TranslationsUpdated + TranslationsDeleted > 0;
}
