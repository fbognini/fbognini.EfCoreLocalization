using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Localizers
{
    internal class EFStringLocalizerFactory : IStringLocalizerFactory, IExtendedStringLocalizerFactory
    {
        private static readonly ConcurrentDictionary<string, IStringLocalizer> _localizers = new();

        private readonly ILocalizationRepository _localizationRepository;
        private readonly EfCoreLocalizationSettings _efCoreLocalizationSettings;

        public EFStringLocalizerFactory(ILocalizationRepository localizationRepository, IOptions<EfCoreLocalizationSettings> efCoreLocalizationOptions)
        {
            _localizationRepository = localizationRepository;
            _efCoreLocalizationSettings = efCoreLocalizationOptions.Value;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            return Create(GetResourceIdFromType(resourceSource));
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return Create(GetCompositeResourceId(baseName, location));
        }

        private IStringLocalizer Create(string resourceId)
        {
            resourceId = NormalizeResourceId(resourceId);

            if (_localizers.TryGetValue(resourceId, out IStringLocalizer? localizer))
            {
                return localizer;
            }

            var newLocalizer = new EFStringLocalizer(GetResources(resourceId), resourceId, _localizationRepository, _efCoreLocalizationSettings.CreateNewRecordWhenDoesNotExists, _efCoreLocalizationSettings.ReturnOnlyKeyIfNotFound);
            return _localizers.GetOrAdd(resourceId, newLocalizer);
        }

        public void ResetCache()
        {
            _localizers.Clear();
            _localizationRepository.DetachAllEntities();
        }

        public void ResetCache(Type resourceSource)
        {
            ResetCache(GetResourceIdFromType(resourceSource));
        }

        public void ResetCache(string baseName, string location)
        {
            ResetCache(GetCompositeResourceId(baseName, location));
        }

        private void ResetCache(string resourceId)
        {
            resourceId = NormalizeResourceId(resourceId);

            _localizers.TryRemove(resourceId, out _);
            _localizationRepository.DetachAllEntities();
        }

        private Dictionary<string, string> GetResources(string resourceId)
        {
            return _localizationRepository.GetTranslations(null, null, resourceId)
                    .ToDictionary(kvp => kvp.TextId + "." + kvp.LanguageId, kvp => kvp.Destination, StringComparer.OrdinalIgnoreCase);
        }

        public string NormalizeResourceId(string key)
        {
            if (!string.IsNullOrWhiteSpace(_efCoreLocalizationSettings.GlobalResourceId))
            {
                return _efCoreLocalizationSettings.GlobalResourceId;
            }

            foreach (var suffix in _efCoreLocalizationSettings.RemoveSuffixs.Where(s => key.EndsWith(s)))
            {
                key = key[..^suffix.Length];
            }

            foreach (var prefix in _efCoreLocalizationSettings.RemovePrefixs.Where(s => key.StartsWith(s)))
            {
                key = key[prefix.Length..];
            }

            if (!string.IsNullOrWhiteSpace(_efCoreLocalizationSettings.ResourceIdPrefix))
            {
                key = $"{_efCoreLocalizationSettings.ResourceIdPrefix}.{key}";
            }

            return key;
        }

        private string GetCompositeResourceId(string baseName, string location)
        {
            string resourceKey = string.IsNullOrWhiteSpace(location) || baseName.StartsWith(location) ? baseName : $"{location}.{baseName}";

            //if (string.IsNullOrWhiteSpace(location))
            //    return baseName;

            //// it's ok for views, be careful for other situations
            //var name = string.Join('.', baseName.Split('.').TakeLast(2));
            //return name;

            return resourceKey;
        }

        public string GetResourceIdFromType(Type resourceSource)
        {
            var attribute = resourceSource.GetCustomAttributes(typeof(LocalizationKeyAttribute), false).SingleOrDefault();
            if (attribute == null)
            {
                return GetRecursiveResourceName(resourceSource);
            }

            return ((LocalizationKeyAttribute)attribute).Key;
        }


        private static string GetRecursiveResourceName(Type resourceSource)
        {
            var builder = GetRecursiveResourceName(resourceSource, new StringBuilder());
            if (builder.Length > 0)
            {
                builder = builder.Remove(builder.Length - 1, 1);
            }

            return builder.ToString();
        }

        private static StringBuilder GetRecursiveResourceName(Type resourceSource, StringBuilder name)
        {
            if (resourceSource == null)
            {
                return name;
            }

            return GetRecursiveResourceName(resourceSource.DeclaringType, name.Insert(0, '.').Insert(0, resourceSource.Name));
        }
    }
}
