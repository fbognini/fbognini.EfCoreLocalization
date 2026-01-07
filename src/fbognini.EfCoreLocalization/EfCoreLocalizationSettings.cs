using System.Collections.Generic;

namespace fbognini.EfCoreLocalization
{
    public class EfCoreLocalizationSettings
    {
        public string? DefaultSchema { get; set; }       

        
        /// <summary>
        /// If GlobalResourceId has a value, it will be used as ResourceId for everything => Only property names are used to find the translations
        /// </summary>
        public string? GlobalResourceId { get; set; }
        public string? ResourceIdPrefix { get; set; }

        public List<string> RemovePrefixsFromTypes { get; set; } = [];
        public List<string> RemoveSuffixsFromTypes { get; set; } = [];

        public bool IgnoreResourceLocation { get; set; }
        public List<string> RemovePrefixsFromLocations { get; set; } = [];


        /// <summary>
        /// Returns only the Key if the value is not found. If set to false, the search key in the database is returned.
        /// </summary>
        public bool ReturnOnlyKeyIfNotFound { get; set; }

        /// <summary>
        /// Creates a new item in the SQL database if the resource is not found
        /// </summary>
        public bool CreateNewRecordWhenDoesNotExists { get; set; }

        /// <summary>
        /// Cache expiration time in minutes. If null, the cache never expires (infinite).
        /// </summary>
        public int? CacheExpirationMinutes { get; set; }
    }
}
