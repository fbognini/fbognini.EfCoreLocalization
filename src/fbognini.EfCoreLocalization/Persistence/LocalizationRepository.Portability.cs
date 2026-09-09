using fbognini.EfCoreLocalization.Persistence.Entities;
using fbognini.EfCoreLocalization.Portability;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Text = fbognini.EfCoreLocalization.Persistence.Entities.Text;

namespace fbognini.EfCoreLocalization.Persistence;

internal partial class LocalizationRepository
{
    private const int MaxTextIdLength = 100;
    private const int MaxResourceIdLength = 50;
    private const int MaxDescriptionLength = 500;

    public TranslationsSnapshot ExportTranslations(TranslationsExportFilter? filter = null)
    {
        filter ??= new TranslationsExportFilter();

        lock (_dbContext)
        {
            // Read from the DbSet rather than from GetLanguages(): the cached list is only refreshed by AddLanguage and UpdateLanguage.
            var languages = _dbContext.Languages.AsNoTracking().ToList();

            if (filter.ActiveLanguagesOnly)
            {
                languages = languages.Where(x => x.IsActive).ToList();
            }

            if (filter.LanguageIds != null && filter.LanguageIds.Count > 0)
            {
                var wanted = new HashSet<string>(filter.LanguageIds.Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
                languages = languages.Where(x => wanted.Contains(x.Id.Trim())).ToList();
            }

            // The default language first, then alphabetically: it is the column order a translator expects.
            var languageIds = languages
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Id.Trim())
                .ToList();

            var snapshot = new TranslationsSnapshot { ExportedOnUtc = DateTime.UtcNow };
            foreach (var languageId in languageIds)
            {
                snapshot.LanguageIds.Add(languageId);
            }

            var knownLanguageIds = new HashSet<string>(languageIds, StringComparer.OrdinalIgnoreCase);

            var textsQuery = _dbContext.Texts.AsNoTracking();
            var translationsQuery = _dbContext.Translations.AsNoTracking();

            if (filter.ResourceIds != null && filter.ResourceIds.Count > 0)
            {
                var resourceIds = filter.ResourceIds.Select(x => x.Trim()).ToList();
                textsQuery = textsQuery.Where(x => resourceIds.Contains(x.ResourceId));
                translationsQuery = translationsQuery.Where(x => resourceIds.Contains(x.ResourceId));
            }

            var rowsByKey = new Dictionary<(string ResourceId, string TextId), TranslationsSnapshotRow>();
            foreach (var text in textsQuery.ToList())
            {
                var row = new TranslationsSnapshotRow
                {
                    ResourceId = text.ResourceId,
                    TextId = text.TextId,
                    Description = string.IsNullOrEmpty(text.Description) ? null : text.Description
                };

                rowsByKey[(text.ResourceId, text.TextId)] = row;
            }

            foreach (var translation in translationsQuery.ToList())
            {
                var languageId = translation.LanguageId.Trim();
                if (!knownLanguageIds.Contains(languageId))
                {
                    continue;
                }

                if (rowsByKey.TryGetValue((translation.ResourceId, translation.TextId), out var row))
                {
                    row.Destinations[languageId] = translation.Destination;
                }
            }

            IEnumerable<TranslationsSnapshotRow> rows = rowsByKey.Values;
            if (!filter.IncludeTextsWithoutTranslations)
            {
                rows = rows.Where(x => x.Destinations.Values.Any(value => !string.IsNullOrEmpty(value)));
            }

            foreach (var row in rows.OrderBy(x => x.ResourceId, StringComparer.Ordinal).ThenBy(x => x.TextId, StringComparer.Ordinal))
            {
                snapshot.Rows.Add(row);
            }

            return snapshot;
        }
    }

    public ImportTranslationsResult ImportTranslations(TranslationsSnapshot snapshot, ImportTranslationsOptions? options = null)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        options ??= new ImportTranslationsOptions();

        var result = new ImportTranslationsResult
        {
            DryRun = options.DryRun,
            TotalRows = snapshot.Rows.Count
        };

        var utcNow = DateTime.UtcNow;

        lock (_dbContext)
        {
            try
            {
                var columns = ResolveLanguageColumns(snapshot, result);
                foreach (var column in columns)
                {
                    result.LanguageIds.Add(column.LanguageId);
                }

                var allowedResourceIds = options.AllowedResourceIds == null
                    ? null
                    : new HashSet<string>(options.AllowedResourceIds.Select(x => x.Trim()), StringComparer.Ordinal);

                var scope = new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in snapshot.Rows)
                {
                    var resourceId = row.ResourceId?.Trim();
                    if (!string.IsNullOrEmpty(resourceId) && (allowedResourceIds == null || allowedResourceIds.Contains(resourceId!)))
                    {
                        scope.Add(resourceId!);
                    }
                }

                foreach (var resourceId in scope.OrderBy(x => x, StringComparer.Ordinal))
                {
                    result.ResourceIds.Add(resourceId);
                }

                var scopedResourceIds = scope.ToList();
                var texts = _dbContext.Texts.Where(x => scopedResourceIds.Contains(x.ResourceId)).ToList();
                var translations = _dbContext.Translations.Where(x => scopedResourceIds.Contains(x.ResourceId)).ToList();

                var textsByKey = new Dictionary<(string ResourceId, string TextId), Text>();
                // The model compares keys ordinally while the database collation is usually case insensitive: without this second index a row that only differs in case would be taken for a new one and the insert would fail the whole batch.
                var textsByKeyIgnoreCase = new Dictionary<(string ResourceId, string TextId), Text>(CaseInsensitiveKeyComparer.Instance);
                foreach (var text in texts)
                {
                    var key = (text.ResourceId, text.TextId);
                    textsByKey[key] = text;
                    textsByKeyIgnoreCase.TryAdd(key, text);
                }

                var translationsByKey = new Dictionary<(string ResourceId, string TextId, string LanguageId), Translation>();
                var translationsByText = new Dictionary<(string ResourceId, string TextId), List<Translation>>();
                foreach (var translation in translations)
                {
                    translationsByKey[(translation.ResourceId, translation.TextId, translation.LanguageId.Trim())] = translation;

                    var textKey = (translation.ResourceId, translation.TextId);
                    if (!translationsByText.TryGetValue(textKey, out var children))
                    {
                        children = [];
                        translationsByText[textKey] = children;
                    }

                    children.Add(translation);
                }

                var matchedTexts = new HashSet<(string ResourceId, string TextId)>();
                var seenInFile = new HashSet<(string ResourceId, string TextId)>();

                foreach (var row in snapshot.Rows)
                {
                    var resourceId = row.ResourceId?.Trim() ?? string.Empty;
                    var textId = row.TextId?.Trim() ?? string.Empty;

                    if (resourceId.Length == 0 || textId.Length == 0)
                    {
                        AddError(result, ImportErrorKind.MissingKey, row, null, "ResourceId and TextId are both required.");
                        continue;
                    }

                    var key = (resourceId, textId);

                    // Claimed before validating, so that a rejected row never lets DeleteNotMatched drop the key it refers to.
                    matchedTexts.Add(key);

                    if (allowedResourceIds != null && !allowedResourceIds.Contains(resourceId))
                    {
                        AddError(result, ImportErrorKind.ResourceNotAllowed, row, null, $"ResourceId '{resourceId}' is not allowed.");
                        continue;
                    }

                    if (textId.Length > MaxTextIdLength)
                    {
                        AddError(result, ImportErrorKind.ValueTooLong, row, null, $"TextId exceeds {MaxTextIdLength} characters.");
                        continue;
                    }

                    if (resourceId.Length > MaxResourceIdLength)
                    {
                        AddError(result, ImportErrorKind.ValueTooLong, row, null, $"ResourceId exceeds {MaxResourceIdLength} characters.");
                        continue;
                    }

                    if (!seenInFile.Add(key))
                    {
                        AddError(result, ImportErrorKind.DuplicateRow, row, null, "The same ResourceId and TextId pair appears more than once.");
                        continue;
                    }

                    if (!textsByKey.TryGetValue(key, out var text))
                    {
                        if (textsByKeyIgnoreCase.TryGetValue(key, out var candidate))
                        {
                            matchedTexts.Add((candidate.ResourceId, candidate.TextId));
                            AddError(result, ImportErrorKind.KeyCaseMismatch, row, null, $"The database stores this key as '{candidate.ResourceId}' / '{candidate.TextId}'.");
                            continue;
                        }

                        if (!options.CreateMissingTexts)
                        {
                            AddError(result, ImportErrorKind.TextNotFound, row, null, "The key does not exist and CreateMissingTexts is disabled.");
                            continue;
                        }

                        text = new Text
                        {
                            ResourceId = resourceId,
                            TextId = textId,
                            Description = string.Empty,
                            CreatedOnUtc = utcNow,
                            Translations = []
                        };

                        if (!string.IsNullOrEmpty(row.Description))
                        {
                            if (row.Description!.Length > MaxDescriptionLength)
                            {
                                AddError(result, ImportErrorKind.ValueTooLong, row, null, $"Description exceeds {MaxDescriptionLength} characters and was not applied.");
                            }
                            else
                            {
                                text.Description = row.Description;
                            }
                        }

                        _dbContext.Texts.Add(text);
                        textsByKey[key] = text;
                        textsByKeyIgnoreCase.TryAdd(key, text);
                        result.TextsCreated++;
                    }
                    else if (!string.IsNullOrEmpty(row.Description) && !string.Equals(text.Description, row.Description, StringComparison.Ordinal))
                    {
                        if (row.Description!.Length > MaxDescriptionLength)
                        {
                            AddError(result, ImportErrorKind.ValueTooLong, row, null, $"Description exceeds {MaxDescriptionLength} characters and was not applied.");
                        }
                        else
                        {
                            text.Description = row.Description;
                            result.TextsUpdated++;
                        }
                    }

                    foreach (var column in columns)
                    {
                        row.Destinations.TryGetValue(column.Column, out var destination);
                        if (string.IsNullOrEmpty(destination))
                        {
                            result.TranslationsSkipped++;
                            continue;
                        }

                        var translationKey = (resourceId, textId, column.LanguageId);
                        if (translationsByKey.TryGetValue(translationKey, out var translation))
                        {
                            if (string.Equals(translation.Destination, destination, StringComparison.Ordinal))
                            {
                                result.TranslationsUnchanged++;
                                continue;
                            }

                            translation.Destination = destination!;
                            translation.UpdatedOnUtc = utcNow;
                            result.TranslationsUpdated++;
                            continue;
                        }

                        if (!options.CreateMissingTranslations)
                        {
                            AddError(result, ImportErrorKind.TranslationNotFound, row, column.LanguageId, "The translation does not exist and CreateMissingTranslations is disabled.");
                            continue;
                        }

                        translation = new Translation
                        {
                            // Written exactly as the Language row stores it: a provider that does not pad nchar(5) would otherwise break the foreign key.
                            LanguageId = column.StorageId,
                            ResourceId = resourceId,
                            TextId = textId,
                            Destination = destination!,
                            UpdatedOnUtc = utcNow
                        };

                        text.Translations?.Add(translation);
                        _dbContext.Translations.Add(translation);
                        translationsByKey[translationKey] = translation;
                        result.TranslationsAdded++;
                    }
                }

                if (options.DeleteNotMatched)
                {
                    foreach (var text in texts)
                    {
                        if (matchedTexts.Contains((text.ResourceId, text.TextId)))
                        {
                            continue;
                        }

                        if (translationsByText.TryGetValue((text.ResourceId, text.TextId), out var children))
                        {
                            // Removed explicitly rather than relying on the cascade, so that they can be counted.
                            _dbContext.Translations.RemoveRange(children);
                            result.TranslationsDeleted += children.Count;
                        }

                        _dbContext.Texts.Remove(text);
                        result.TextsDeleted++;
                    }
                }

                if (!options.DryRun && result.HasChanges)
                {
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (DbUpdateException exception)
                    {
                        throw new LocalizationImportException("The translations import could not be saved.", result, exception);
                    }
                }
            }
            finally
            {
                // The DbContext is a singleton: leaving spurious entries behind would poison every later SaveChanges of the process.
                _dbContext.DetachAllEntities();
            }
        }

        return result;
    }

    private List<LanguageColumn> ResolveLanguageColumns(TranslationsSnapshot snapshot, ImportTranslationsResult result)
    {
        var languages = _dbContext.Languages.AsNoTracking().Select(x => x.Id).ToList();

        var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var byTwoLetters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in languages)
        {
            var id = language.Trim();
            byId[id] = language;

            var prefix = id.Length >= 2 ? id.Substring(0, 2) : id;
            if (!byTwoLetters.TryGetValue(prefix, out var matches))
            {
                matches = [];
                byTwoLetters[prefix] = matches;
            }

            matches.Add(language);
        }

        var columns = new List<LanguageColumn>();
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unknown = new List<string>();

        foreach (var column in snapshot.LanguageIds)
        {
            var candidate = column.Trim();

            string? storageId = null;
            if (byId.TryGetValue(candidate, out var exact))
            {
                storageId = exact;
            }
            else if (byTwoLetters.TryGetValue(candidate, out var matches) && matches.Count == 1)
            {
                storageId = matches[0];
            }

            if (storageId == null)
            {
                unknown.Add(candidate);
                result.Errors.Add(new ImportTranslationsError
                {
                    Kind = ImportErrorKind.UnknownLanguage,
                    SourceRow = 0,
                    LanguageId = candidate,
                    Reason = $"'{candidate}' does not match any language and the whole column was ignored."
                });

                continue;
            }

            if (resolved.Add(storageId.Trim()))
            {
                columns.Add(new LanguageColumn(column, storageId.Trim(), storageId));
            }
        }

        if (columns.Count == 0 && snapshot.LanguageIds.Count > 0)
        {
            throw new ArgumentException($"None of the language columns could be resolved: [{string.Join(", ", unknown)}].", nameof(snapshot));
        }

        return columns;
    }

    private static void AddError(ImportTranslationsResult result, ImportErrorKind kind, TranslationsSnapshotRow row, string? languageId, string reason)
    {
        result.Errors.Add(new ImportTranslationsError
        {
            Kind = kind,
            SourceRow = row.SourceRow,
            ResourceId = row.ResourceId,
            TextId = row.TextId,
            LanguageId = languageId,
            Reason = reason
        });
    }

    private sealed record LanguageColumn(string Column, string LanguageId, string StorageId);

    private sealed class CaseInsensitiveKeyComparer : IEqualityComparer<(string ResourceId, string TextId)>
    {
        public static readonly CaseInsensitiveKeyComparer Instance = new();

        public bool Equals((string ResourceId, string TextId) x, (string ResourceId, string TextId) y)
        {
            return string.Equals(x.ResourceId, y.ResourceId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.TextId, y.TextId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string ResourceId, string TextId) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ResourceId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TextId));
        }
    }
}
