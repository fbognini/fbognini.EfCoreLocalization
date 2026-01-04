using fbognini.Core.Domain.Query;
using fbognini.EfCoreLocalization.Persistence.Entities;
using System.Linq.Expressions;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Translations
{
    internal class TranslationSelectCriteria : QueryableCriteria<Translation>
    {
        public string? LanguageId { get; set; }
        public string? TextId { get; set; }
        public string? ResourceId { get; set; }
        public bool? NotTranslated { get; set; }

        public override List<Expression<Func<Translation, bool>>> ToWhereClause()
        {
            var list = new List<Expression<Func<Translation, bool>>>();

            if (!string.IsNullOrEmpty(LanguageId))
            {
                list.Add(x => x.LanguageId == LanguageId);
            }

            if (!string.IsNullOrEmpty(TextId))
            {
                list.Add(x => x.TextId == TextId);
            }

            if (!string.IsNullOrEmpty(ResourceId))
            {
                list.Add(x => x.ResourceId == ResourceId);
            }

            if (NotTranslated is not null)
            {
                list.Add(x => NotTranslated.Value && x.TextId == x.Destination || !NotTranslated.Value && x.TextId != x.Destination);
            }

            return list;
        }
    }
}
