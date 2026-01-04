namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Translations
{
    public class UpdateTranslationCommand
    {
        public required string LanguageId { get; set; }
        public required string TextId { get; set; }
        public required string ResourceId { get; set; }
        public required string Destination { get; set; }
    }
}
