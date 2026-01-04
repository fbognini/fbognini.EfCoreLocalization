using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Texts;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Mvc;

namespace fbognini.EfCoreLocalization.Dashboard.Areas.i18n.Controllers
{
    public class ApiTextController : BaseApiController
    {
        public ApiTextController(ILocalizationRepository localizationRepository)
            : base(localizationRepository)
        {
        }

        [HttpGet]
        public ActionResult Search([FromQuery] GetPaginatedTextsQuery query, [FromQuery] FullSearchQueryParameters search)
        {
            var criteria = new TextSelectCriteria()
            {
                TextId = query.TextId,
                ResourceId = query.ResourceId,
            };
            criteria.LoadFullSearch(search.ToFullSearch());

            var response = LocalizationRepository.GetPaginatedTexts(criteria);

            var result = new PaginationResponse<TextDto>()
            {
                Items = response.Items.Select(x => x.ToDto()).ToList(),
                Pagination = response.Pagination,
            };

            return Ok(result);
        }

        [HttpPost]
        public ActionResult Create([FromBody] CreateTextCommand command)
        {
            var languages = LocalizationRepository.GetLanguages();
            var translations = languages.ToDictionary(keySelector: language => language.Id, elementSelector: _ => command.TextId);
            
            var texts = LocalizationRepository.AddTranslations(command.TextId, command.ResourceId, command.Description, translations);

            return Ok(texts.First().Text.ToDto());
        }

        [HttpDelete]
        public ActionResult Delete([FromQuery] DeleteTextCommand command)
        {
            LocalizationRepository.DeleteTranslations(command.TextId, command.ResourceId);
            return Ok();
        }
    }
}