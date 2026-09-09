using System;

namespace fbognini.EfCoreLocalization.Portability;

public class LocalizationImportException : Exception
{
    public LocalizationImportException(string message, ImportTranslationsResult partialResult, Exception? innerException = null)
        : base(message, innerException)
    {
        PartialResult = partialResult;
    }

    public ImportTranslationsResult PartialResult { get; }
}
