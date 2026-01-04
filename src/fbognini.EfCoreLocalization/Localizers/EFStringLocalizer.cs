using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.EfCoreLocalization.Localizers
{
    public class EFStringLocalizer: IStringLocalizer
    {
        private readonly Dictionary<string, string> _translations;
        private readonly string _resourceKey;
        private readonly bool _createNewRecordWhenDoesNotExists;
        private readonly bool _returnOnlyKeyIfNotFound;
        private readonly ILocalizationRepository _repository;

        public EFStringLocalizer(Dictionary<string, string> translations, string key, ILocalizationRepository repository, bool createNewRecordWhenDoesNotExists, bool returnOnlyKeyIfNotFound)
        {
            _translations = translations;
            _resourceKey = key;
            _repository = repository;
            _createNewRecordWhenDoesNotExists = createNewRecordWhenDoesNotExists;
            _returnOnlyKeyIfNotFound = returnOnlyKeyIfNotFound;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var text = GetText(name, out var error);
                return new LocalizedString(name, text, error);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var localizedString = this[name];
                return new LocalizedString(name, string.Format(localizedString.Value, arguments), localizedString.ResourceNotFound);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _translations.Select(x => new LocalizedString(x.Key, x.Value));
        }

        private string GetText(string id, out bool error)
        {

#if NET451
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
#elif NET46
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
#else
            var culture = CultureInfo.CurrentCulture;
#endif

            string computedKey = $"{id}.{culture}";
            string parentComputedKey = $"{id}.{culture.Parent.TwoLetterISOLanguageName}";

            if (_translations.TryGetValue(computedKey, out string? translation) || _translations.TryGetValue(parentComputedKey, out translation))
            {
                error = false;
                return translation;
            }

            error = true;
            
            if (_createNewRecordWhenDoesNotExists)
            {
                var cultures = _repository.AddTranslations(id, _resourceKey, string.Empty, new Dictionary<string, string>() { [culture.ToString()] = id });
                foreach (var item in cultures)
                {
                    _translations.Add($"{id}.{item.LanguageId}", id);
                }

                return id;
            }

            if (_returnOnlyKeyIfNotFound)
            {
                return id;
            }

            //if (_returnKeyOnlyIfNotFound)
            //{
            //    return key;
            //}

            return _resourceKey + "." + computedKey;
        }

    }
}
