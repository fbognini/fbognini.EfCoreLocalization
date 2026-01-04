using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Dashboard
{
    public class DashboardContext
    {
        public DashboardContext(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public HttpContext HttpContext { get; set; }
    }
}
