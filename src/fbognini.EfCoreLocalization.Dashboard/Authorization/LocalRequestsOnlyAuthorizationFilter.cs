using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Dashboard.Authorization
{
    public class LocalRequestsOnlyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var remoteIpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var localIpAddress = context.HttpContext.Connection.LocalIpAddress?.ToString();

            // if unknown, assume not local
            if (string.IsNullOrEmpty(remoteIpAddress))
            {
                return false;
            }

            // check if localhost
            if (remoteIpAddress == "127.0.0.1" || remoteIpAddress == "::1")
            {
                return true;
            }

            // compare with local address
            if (remoteIpAddress == localIpAddress)
            {
                return true;
            }

            return false;
        }
    }
}
