using fbognini.WebFramework.FullSearch;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Translations
{
    public class GetPaginatedTranslationsQuery
    {
        public string? LanguageId { get; set; }
        public string? TextId { get; set; }
        public string? ResourceId { get; set; }
        public bool OnlyNotTranslated { get; set; } = false;
    }
}
