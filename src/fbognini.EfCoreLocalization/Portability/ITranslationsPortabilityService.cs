using System.Collections.Generic;
using System.IO;

namespace fbognini.EfCoreLocalization.Portability;

public interface ITranslationsPortabilityService
{
    IReadOnlyList<ITranslationsFormat> Formats { get; }

    /// <summary>
    /// Resolves a format by name, by file extension or by file name. Falls back to CSV when nothing is given.
    /// </summary>
    ITranslationsFormat ResolveFormat(string? nameOrExtension);

    void Export(Stream destination, TranslationsExportFilter? filter = null, string? format = null);

    /// <summary>
    /// Applies a file to the database and, unless nothing changed, drops the localizer cache so that the new texts are served right away.
    /// </summary>
    ImportTranslationsResult Import(Stream source, ImportTranslationsOptions? options = null, string? format = null);
}
