namespace fbognini.EfCoreLocalization.Portability;

public enum ImportErrorKind
{
    MissingKey,
    ValueTooLong,
    ResourceNotAllowed,
    UnknownLanguage,
    DuplicateRow,
    KeyCaseMismatch,
    TextNotFound,
    TranslationNotFound
}
