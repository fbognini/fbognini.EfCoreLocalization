using fbognini.Core.Domain.Query;
using fbognini.Core.Domain.Query.Pagination;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Text = fbognini.EfCoreLocalization.Persistence.Entities.Text;

namespace fbognini.EfCoreLocalization.Persistence;

public interface ILocalizationRepository
{
    IEnumerable<Language> GetLanguages();
    PaginationResponse<Language> GetPaginatedLanguages(QueryableCriteria<Language> criteria);
    void AddLanguage(Language language);
    Language UpdateLanguage(string id, string description, bool isActive, bool isDefault);

    IEnumerable<Translation> AddTranslations(string textId, string resourceId, string description, Dictionary<string, string> translations);
    void DeleteTranslations(string textId, string resourceId);
    IEnumerable<Translation> GetTranslations(string? languageId, string? textId, string? resourceId, DateTime? since = null);
    Translation? GetTranslation(string languageId, string textId, string resourceId);
    PaginationResponse<Translation> GetPaginatedTranslations(QueryableCriteria<Translation> criteria);
    PaginationResponse<Text> GetPaginatedTexts(QueryableCriteria<Text> criteria);

    void UpdateTranslation(Translation translation);
    void UpdateTranslations(List<Translation> translations);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="translations"></param>
    /// <param name="all">if true, import all rows, otherwise import only updated rows</param>
    /// <param name="deleteNotMatched">if true, delete translation when not found</param>
    void ImportTranslations(IEnumerable<Translation> translations, bool all, bool deleteNotMatched);

    internal void DetachAllEntities();
}

internal class LocalizationRepository : ILocalizationRepository
{
    private List<Language>? _languages;
    private readonly EfCoreLocalizationDbContext _dbContext;

    public LocalizationRepository(EfCoreLocalizationDbContext context)
    {
        _dbContext = context;
    }

    public IEnumerable<Language> GetLanguages()
    {
        if (_languages == null)
        {
            lock (_dbContext)
            {
                LoadLanguages();
            }
        }

        return _languages!;
    }

    public PaginationResponse<Language> GetPaginatedLanguages(QueryableCriteria<Language> criteria) => GetPaginatedResponse<Language>(criteria);

    public void AddLanguage(Language language)
    {
        lock (_dbContext)
        {
           
            var existingDefault = _dbContext.Languages.FirstOrDefault(x => x.IsDefault) ?? _dbContext.Languages.FirstOrDefault();
            if (existingDefault == null)
            {
                language.IsDefault = true;
                _dbContext.Languages.Add(language);
                _dbContext.SaveChanges();

                LoadLanguages();
                return;
            }


            if (language.IsDefault)
            {
                existingDefault.IsDefault = false;
            }

            _dbContext.Languages.Add(language);
            _dbContext.SaveChanges();
            _dbContext.SaveChanges();

            var defaultSchema = _dbContext.Model.GetDefaultSchema();

            var schemaWithPrefix = string.IsNullOrWhiteSpace(defaultSchema) ? "" : $"[{defaultSchema}].";

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
            _dbContext.Database.ExecuteSqlRaw($"""
                INSERT INTO {schemaWithPrefix}Translations (LanguageId, TextId, ResourceId, Destination, UpdatedOnUtc)
                SELECT '{language.Id}', TextId, ResourceId, Destination, GETUTCDATE()
                FROM {schemaWithPrefix}Translations t
                WHERE t.LanguageId = '{existingDefault.Id}'
                """);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

            LoadLanguages();
        }
    }

    public Language UpdateLanguage(string id, string description, bool isActive, bool isDefault)
    {
        var language = _dbContext.Languages.Find(id);
        if (language is null)
        {
            throw new ArgumentException($"Invalid language {id}");
        }

        language.Description = description;
        language.IsActive = isActive;
        language.IsDefault = isDefault;

        lock (_dbContext)
        {
            _dbContext.Languages.Update(language);
            _dbContext.SaveChanges();

            LoadLanguages();
        }

        return language;
    }

    public IEnumerable<Translation> GetTranslations(string? languageId, string? textId, string? resourceId, DateTime? since = null)
    {
        (this as ILocalizationRepository).DetachAllEntities();

        lock (_dbContext)
        {
            var query = _dbContext.Translations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(languageId))
            {
                query = query.Where(x => x.LanguageId == languageId);
            }
            if (!string.IsNullOrWhiteSpace(textId))
            {
                query = query.Where(x => x.TextId == textId);
            }
            if (!string.IsNullOrWhiteSpace(resourceId))
            {
                query = query.Where(x => x.ResourceId == resourceId);
            }
            if (since.HasValue)
            {
                query = query.Where(x => x.UpdatedOnUtc >= since.Value);
            }

            return query.ToList();
        }
    }

    public Translation? GetTranslation(string languageId, string textId, string resourceId)
    {
        return _dbContext.Translations.Find(languageId, textId, resourceId);
    }

    public PaginationResponse<Translation> GetPaginatedTranslations(QueryableCriteria<Translation> criteria) => GetPaginatedResponse<Translation>(criteria);

    public PaginationResponse<Text> GetPaginatedTexts(QueryableCriteria<Text> criteria) => GetPaginatedResponse<Text>(criteria);

    public IEnumerable<Translation> AddTranslations(string textId, string resourceId, string description, Dictionary<string, string> translations)
    {
        if (translations == null || translations.Count == 0)
        {
            throw new ArgumentException("Translations must be provided");
        }

        var languages = GetLanguages();
        var invalid = translations.Where(t => !languages.Any(l => l.Id == t.Key)).ToList();
        if (invalid.Count != 0)
        {
            throw new ArgumentException($"Invalid languages [{string.Join(", ", invalid.Select(x => x.Key))}]");
        }

        var defaultLanguage = languages.FirstOrDefault(x => x.IsDefault) ?? languages.First();
        var defaultTranslation = translations.TryGetValue(defaultLanguage.Id, out string? value) ? value : translations.First().Value;
        
        foreach (var item in languages.Where(x => !translations.ContainsKey(x.Id)))
        {
            translations.Add(item.Id, defaultTranslation);
        }
        
        var utcNow = DateTime.UtcNow;

        var text = new Text()
        {
            TextId = textId,
            ResourceId = resourceId,
            Description = description,
            CreatedOnUtc = utcNow,
            Translations = [.. translations
                .Select(x => new Translation()
                {
                    TextId = textId,
                    ResourceId = resourceId,
                    LanguageId = x.Key,
                    Destination = x.Value ?? string.Empty,
                    UpdatedOnUtc = utcNow
                })]
        };

        lock (_dbContext)
        {
            _dbContext.Texts.Add(text);
            _dbContext.SaveChanges();
        }

        return text.Translations;
    }

    public void DeleteTranslations(string textId, string resourceId)
    {
        lock (_dbContext)
        {
            _dbContext.Translations.RemoveRange(_dbContext.Translations.Where(x => x.TextId == textId && x.ResourceId == resourceId));
            var text = _dbContext.Texts.Find(textId, resourceId);
            if (text is not null)
            {
                _dbContext.Texts.Remove(text);
            }
            _dbContext.SaveChanges();
        }
    }

    public void UpdateTranslation(Translation translation)
    {
        UpdateTranslation(translation, true);
    }

    public void UpdateTranslations(List<Translation> translations)
    {
        foreach (var translation in translations)
        {
            UpdateTranslation(translation, false);
        }

        lock (_dbContext)
        {
            _dbContext.SaveChanges();
        }
    }

    public void ImportTranslations(IEnumerable<Translation> translations, bool all, bool deleteNotMatched)
    {
        var utcNow = DateTime.UtcNow;

        lock (_dbContext)
        {
            var existing = GetTranslations(null, null, null, null);
            foreach (var existingTranslation in existing)
            {
                var newTranslation = translations
                    .FirstOrDefault(x => x.LanguageId == existingTranslation.LanguageId && x.ResourceId == existingTranslation.ResourceId && x.TextId == existingTranslation.TextId);

                if (newTranslation == null)
                {
                    if (deleteNotMatched)
                    {
                        _dbContext.Translations.Remove(existingTranslation);
                    }

                    continue;
                }

                if (!all && existingTranslation.UpdatedOnUtc > newTranslation.UpdatedOnUtc)
                {
                    continue;
                }

                if (!existingTranslation.Destination.Equals(newTranslation.Destination))
                {
                    existingTranslation.UpdatedOnUtc = utcNow;
                    existingTranslation.Destination = newTranslation.Destination;
                }
            }

            _dbContext.SaveChanges();
        }
    }

    void ILocalizationRepository.DetachAllEntities()
    {
        lock (_dbContext)
        {
            _dbContext.DetachAllEntities();
        }
    }

    private void LoadLanguages()
    {
        _languages = [.. _dbContext.Languages];
    }

    private void UpdateTranslation(Translation translation, bool saveChanges = true)
    {
        var entity = _dbContext.Translations.Find(translation.LanguageId, translation.TextId, translation.ResourceId);
        if (entity == null)
            return;

        entity.Destination = translation.Destination;
        entity.UpdatedOnUtc = DateTime.UtcNow;
        _dbContext.Translations.Update(entity);

        if (!saveChanges)
            return;

        lock (_dbContext)
        {
            _dbContext.SaveChanges();
        }
    }

    private PaginationResponse<T> GetPaginatedResponse<T>(QueryableCriteria<T> criteria)
        where T : class
    {
        lock (_dbContext)
        {
            var query = _dbContext.Set<T>().QuerySelect(criteria).QuerySearch(criteria, out var pagination);
            var list = query.ToList();
            var response = new PaginationResponse<T>()
            {
                Pagination = pagination,
                Items = list
            };

            return response;
        }
    }
}
