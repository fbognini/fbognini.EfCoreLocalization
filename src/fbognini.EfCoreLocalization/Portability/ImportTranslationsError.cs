namespace fbognini.EfCoreLocalization.Portability;

public class ImportTranslationsError
{
    public ImportErrorKind Kind { get; set; }

    public int SourceRow { get; set; }

    public string? ResourceId { get; set; }

    public string? TextId { get; set; }

    public string? LanguageId { get; set; }

    public string Reason { get; set; } = null!;
}
