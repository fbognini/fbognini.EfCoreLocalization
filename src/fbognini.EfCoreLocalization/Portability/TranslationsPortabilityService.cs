using fbognini.EfCoreLocalization.Localizers;
using fbognini.EfCoreLocalization.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fbognini.EfCoreLocalization.Portability;

internal class TranslationsPortabilityService : ITranslationsPortabilityService
{
    private const string DefaultFormatName = "csv";

    private readonly ILocalizationRepository _repository;
    private readonly IExtendedStringLocalizerFactory _localizerFactory;

    public TranslationsPortabilityService(
        ILocalizationRepository repository,
        IExtendedStringLocalizerFactory localizerFactory,
        IEnumerable<ITranslationsFormat> formats)
    {
        _repository = repository;
        _localizerFactory = localizerFactory;
        Formats = formats.ToList();
    }

    public IReadOnlyList<ITranslationsFormat> Formats { get; }

    public ITranslationsFormat ResolveFormat(string? nameOrExtension)
    {
        if (Formats.Count == 0)
        {
            throw new InvalidOperationException($"No {nameof(ITranslationsFormat)} is registered.");
        }

        if (string.IsNullOrWhiteSpace(nameOrExtension))
        {
            return Formats.FirstOrDefault(x => string.Equals(x.Name, DefaultFormatName, StringComparison.OrdinalIgnoreCase)) ?? Formats[0];
        }

        var candidate = nameOrExtension!.Trim();
        var format = Match(candidate);
        if (format != null)
        {
            return format;
        }

        var extension = Path.GetExtension(candidate);
        if (!string.IsNullOrEmpty(extension))
        {
            format = Match(extension);
            if (format != null)
            {
                return format;
            }
        }

        throw new NotSupportedException($"Unknown translations format '{nameOrExtension}'. Available formats: [{string.Join(", ", Formats.Select(x => x.Name))}].");
    }

    public void Export(Stream destination, TranslationsExportFilter? filter = null, string? format = null)
    {
        var translationsFormat = ResolveFormat(format);
        translationsFormat.Write(_repository.ExportTranslations(filter), destination);
    }

    public ImportTranslationsResult Import(Stream source, ImportTranslationsOptions? options = null, string? format = null)
    {
        var translationsFormat = ResolveFormat(format);
        var snapshot = translationsFormat.Read(source);

        var result = _repository.ImportTranslations(snapshot, options);

        // The localizers materialize their strings once, so without this the import would have no effect until CacheExpirationMinutes elapses.
        if (!result.DryRun && result.HasChanges)
        {
            _localizerFactory.ResetCache();
        }

        return result;
    }

    private ITranslationsFormat? Match(string candidate)
    {
        return Formats.FirstOrDefault(x =>
            string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.FileExtension, candidate, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.FileExtension.TrimStart('.'), candidate, StringComparison.OrdinalIgnoreCase)
            || x.Aliases.Any(alias => string.Equals(alias, candidate, StringComparison.OrdinalIgnoreCase)));
    }
}
