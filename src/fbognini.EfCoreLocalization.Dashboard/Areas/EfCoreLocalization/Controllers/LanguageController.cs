using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Dashboard.Handlers.Languages;
using fbognini.EfCoreLocalization.Dashboard.Helpers;
using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using fbognini.WebFramework.FullSearch;
using Microsoft.AspNetCore.Mvc;

namespace fbognini.EfCoreLocalization.Dashboard.Areas.i18n.Controllers
{
    public class ApiLanguageController : BaseApiController
    {
        public ApiLanguageController(ILocalizationRepository localizationRepository)
            : base(localizationRepository)
        {
        }

        [HttpGet]
        public ActionResult Search([FromQuery] GetPaginatedLanguagesQuery query, [FromQuery] FullSearchQueryParameters search)
        {
            var criteria = new LanguageSelectCriteria();
            criteria.LoadFullSearch(search.ToFullSearch());

            var response = LocalizationRepository.GetPaginatedLanguages(criteria);

            var result = new PaginationResponse<LanguageDto>()
            {
                Items = [.. response.Items.Select(x => x.ToDto())],
                Pagination = response.Pagination,
            };

            return Ok(result);
        }

        [HttpPost]
        public ActionResult Create([FromBody] CreateLanguageCommand command)
        {
            var language = new Language()
            {
                Id = command.Id,
                Description = command.Description,
                IsActive = command.IsActive,
                IsDefault = command.IsDefault
            };

            LocalizationRepository.AddLanguage(language);

            return Ok(language.ToDto());
        }
        
        [HttpPut]
        public ActionResult Update([FromBody] UpdateLanguageCommand command)
        {
            var language = LocalizationRepository.UpdateLanguage(command.Id, command.Description, command.IsActive, command.IsDefault);

            return Ok(language.ToDto());
        }
    }
}