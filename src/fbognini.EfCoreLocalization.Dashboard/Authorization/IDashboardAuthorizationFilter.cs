using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Dashboard.Authorization
{
    public interface IDashboardAuthorizationFilter
    {
        bool Authorize(DashboardContext context);
    }

    public interface IDashboardAsyncAuthorizationFilter
    {
        Task<bool> AuthorizeAsync(DashboardContext context);
    }
}
