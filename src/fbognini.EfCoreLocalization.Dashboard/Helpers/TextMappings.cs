using fbognini.EfCoreLocalization.Dashboard.Handlers.Languages;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Texts;
using fbognini.EfCoreLocalization.Persistence.Entities;

namespace fbognini.EfCoreLocalization.Dashboard.Helpers
{
    internal static class TextMappings
    {
        public static TextDto ToDto(this Text text) => new TextDto()
        {
            TextId = text.TextId,
            ResourceId = text.ResourceId,
            Description = text.Description,
            CreatedOnUtc = text.CreatedOnUtc,
        };
    }
}
