using fbognini.EfCoreLocalization.Dashboard.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Dashboard
{
    public class DashboardOptions
    {
        private static readonly IDashboardAuthorizationFilter[] DefaultAuthorization = new[] { new LocalRequestsOnlyAuthorizationFilter() };

        private IEnumerable<IDashboardAsyncAuthorizationFilter> _asyncAuthorization;

        public DashboardOptions()
        {
            Authorization = DefaultAuthorization;
            _asyncAuthorization = Array.Empty<IDashboardAsyncAuthorizationFilter>();
        }

        public IEnumerable<IDashboardAuthorizationFilter> Authorization { get; set; }

        /// <summary>
        /// Maximum size accepted by the import endpoint. Defaults to 10 MB.
        /// </summary>
        public long MaxImportFileSizeBytes { get; set; } = 10 * 1024 * 1024;
        public IEnumerable<IDashboardAsyncAuthorizationFilter> AsyncAuthorization
        {
            get => _asyncAuthorization;
            set
            {
                _asyncAuthorization = value;

                if (ReferenceEquals(Authorization, DefaultAuthorization))
                {
                    Authorization = [];
                }
            }
        }
    }
}
