using System.Collections.Generic;

namespace fbognini.EfCoreLocalization.Portability;

public class TranslationsExportFilter
{
    public ICollection<string>? ResourceIds { get; set; }

    public ICollection<string>? LanguageIds { get; set; }

    public bool ActiveLanguagesOnly { get; set; } = true;

    public bool IncludeTextsWithoutTranslations { get; set; } = true;
}
