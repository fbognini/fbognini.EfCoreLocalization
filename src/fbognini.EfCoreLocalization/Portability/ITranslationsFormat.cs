using System.Collections.Generic;
using System.IO;

namespace fbognini.EfCoreLocalization.Portability;

public interface ITranslationsFormat
{
    string Name { get; }

    string ContentType { get; }

    string FileExtension { get; }

    /// <summary>
    /// Extra names accepted when resolving the format, on top of Name and FileExtension.
    /// </summary>
    IReadOnlyCollection<string> Aliases => [];

    /// <summary>
    /// Reads a snapshot without resolving languages or validating resource ids: all the semantic validation lives in the import.
    /// </summary>
    TranslationsSnapshot Read(Stream stream);

    void Write(TranslationsSnapshot snapshot, Stream stream);
}
