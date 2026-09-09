using fbognini.EfCoreLocalization.Persistence;
using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace fbognini.EfCoreLocalization.Tests;

/// <summary>
/// A real relational database rather than the InMemory provider: key constraints and delete ordering are exactly what these tests exercise.
/// </summary>
public sealed class LocalizationTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public LocalizationTestHost(Action<EfCoreLocalizationSettings>? configure = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var settings = new EfCoreLocalizationSettings();
        configure?.Invoke(settings);

        var options = new DbContextOptionsBuilder<EfCoreLocalizationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new EfCoreLocalizationDbContext(options, Options.Create(settings));
        Context.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton(Context);
        services.AddEfCoreLocalization(_ => { });
        _provider = services.BuildServiceProvider();

        Repository = _provider.GetRequiredService<ILocalizationRepository>();
    }

    public IServiceProvider Services => _provider;

    public EfCoreLocalizationDbContext Context { get; }

    public ILocalizationRepository Repository { get; }

    public LocalizationTestHost SeedLanguage(string id, bool isDefault = false, bool isActive = true)
    {
        Context.Languages.Add(new Language
        {
            Id = id,
            Description = id.Trim(),
            IsActive = isActive,
            IsDefault = isDefault
        });

        Context.SaveChanges();
        Context.DetachAllEntities();
        return this;
    }

    public LocalizationTestHost SeedText(string resourceId, string textId, string description = "", params (string LanguageId, string Destination)[] translations)
    {
        return SeedText(resourceId, textId, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), description, translations);
    }

    public LocalizationTestHost SeedText(string resourceId, string textId, DateTime updatedOnUtc, string description, params (string LanguageId, string Destination)[] translations)
    {
        Context.Texts.Add(new Text
        {
            ResourceId = resourceId,
            TextId = textId,
            Description = description,
            CreatedOnUtc = updatedOnUtc,
            Translations = translations
                .Select(x => new Translation
                {
                    ResourceId = resourceId,
                    TextId = textId,
                    LanguageId = x.LanguageId,
                    Destination = x.Destination,
                    UpdatedOnUtc = updatedOnUtc
                })
                .ToList()
        });

        Context.SaveChanges();
        Context.DetachAllEntities();
        return this;
    }

    public List<Translation> Translations()
    {
        Context.DetachAllEntities();
        return Context.Translations.AsNoTracking().ToList();
    }

    public Translation? Translation(string resourceId, string textId, string languageId)
    {
        return Translations().SingleOrDefault(x => x.ResourceId == resourceId && x.TextId == textId && x.LanguageId.Trim() == languageId);
    }

    public List<Text> Texts()
    {
        Context.DetachAllEntities();
        return Context.Texts.AsNoTracking().ToList();
    }

    public void Dispose()
    {
        _provider.Dispose();
        Context.Dispose();
        _connection.Dispose();
    }
}
