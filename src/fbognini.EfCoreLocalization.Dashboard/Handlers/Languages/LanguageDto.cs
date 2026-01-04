using fbognini.EfCoreLocalization.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Languages
{
    public class LanguageDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }
}
