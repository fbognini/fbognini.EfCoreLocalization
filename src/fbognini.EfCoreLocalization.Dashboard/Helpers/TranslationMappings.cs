using fbognini.EfCoreLocalization.Dashboard.Handlers.Translations;
using fbognini.EfCoreLocalization.Persistence.Entities;

namespace fbognini.EfCoreLocalization.Dashboard.Helpers
{
    internal static class TranslationMappings
    {
        public static TranslationDto ToDto(this Translation translation)
        {
            var dto = new TranslationDto()
            {
                LanguageId = translation.LanguageId,
                TextId = translation.TextId,
                ResourceId = translation.ResourceId,
                Destination = translation.Destination,
                UpdatedOnUtc = translation.UpdatedOnUtc,
            };

            return dto;
        }
    }
}
