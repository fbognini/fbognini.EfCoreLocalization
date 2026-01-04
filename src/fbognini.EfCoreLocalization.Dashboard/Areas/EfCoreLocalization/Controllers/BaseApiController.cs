using fbognini.EfCoreLocalization.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace fbognini.EfCoreLocalization.Dashboard.Areas.i18n.Controllers
{
    [Area(DashboardContants.Area)]
    public abstract class BaseApiController : Controller
    {
        protected readonly ILocalizationRepository LocalizationRepository;

        protected BaseApiController(ILocalizationRepository localizationRepository)
        {
            LocalizationRepository = localizationRepository;
        }
    }
}
