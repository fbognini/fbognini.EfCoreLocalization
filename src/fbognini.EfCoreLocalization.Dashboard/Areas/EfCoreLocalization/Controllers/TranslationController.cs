using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Translations;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Mvc;

namespace fbognini.EfCoreLocalization.Dashboard.Areas.i18n.Controllers
{
    public class ApiTranslationController : BaseApiController
    {
        public ApiTranslationController(ILocalizationRepository localizationRepository)
            : base(localizationRepository)
        {
        }

        [HttpGet]
        public ActionResult Search([FromQuery] GetPaginatedTranslationsQuery query, [FromQuery] FullSearchQueryParameters search)
        {
            var criteria = new TranslationSelectCriteria()
            {
                LanguageId = query.LanguageId,
                TextId = query.TextId,
                ResourceId = query.ResourceId,
                NotTranslated = query.OnlyNotTranslated ? true : null,
            };
            criteria.LoadFullSearch(search.ToFullSearch());

            var response = LocalizationRepository.GetPaginatedTranslations(criteria);

            var result = new PaginationResponse<TranslationDto>()
            {
                Items = response.Items.Select(x => x.ToDto()).ToList(),
                Pagination = response.Pagination,
            };

            return Ok(result);
        }

        [HttpPut]
        public ActionResult Update([FromBody] UpdateTranslationCommand command)
        {
            var translation = new Translation()
            {
                LanguageId = command.LanguageId,
                TextId = command.TextId,
                ResourceId = command.ResourceId,
                Destination = command.Destination,
            };
            LocalizationRepository.UpdateTranslation(translation);

            var updatedTranslation = LocalizationRepository.GetTranslation(command.LanguageId, command.TextId, command.ResourceId)!;

            return Ok(updatedTranslation.ToDto());
        }
    }
}