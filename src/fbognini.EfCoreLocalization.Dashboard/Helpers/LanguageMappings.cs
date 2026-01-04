using fbognini.EfCoreLocalization.Dashboard.Handlers.Languages;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.AspNetCore.Server.IISIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Dashboard.Helpers
{
    internal static class LanguageMappings
    {
        public static LanguageDto ToDto(this Language language) => new LanguageDto()
        {
            Id = language.Id,
            Description = language.Description,
            IsActive = language.IsActive,
            IsDefault = language.IsDefault
        };
    }
}
