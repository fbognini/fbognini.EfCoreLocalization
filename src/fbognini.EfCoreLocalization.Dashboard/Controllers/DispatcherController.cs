using fbognini.EfCoreLocalization.Dashboard;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Controllers
{
    public class DispatcherController: Controller
    {
        public IActionResult Index(string view)
        {
            if (string.IsNullOrWhiteSpace(view))
            {
                return RedirectToAction(nameof(Index), "Languages", new { Area = DashboardContants.Area });
            }

            return View(view);
        }
    }
}
