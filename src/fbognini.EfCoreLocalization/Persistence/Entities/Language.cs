using System.Collections.Generic;

namespace fbognini.EfCoreLocalization.Persistence.Entities
{
    public class Language
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

        public ICollection<Translation> Translations { get; set; }
    }
}
