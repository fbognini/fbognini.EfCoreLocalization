namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Translations
{
    public class TranslationDto
    {
        public string Id => $"{LanguageId}|{TextId}|{ResourceId}";
        public required string LanguageId { get; set; }
        public required string TextId { get; set; }
        public required string ResourceId { get; set; }
        public required string Destination { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}
