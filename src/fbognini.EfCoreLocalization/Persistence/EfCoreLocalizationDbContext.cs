using fbognini.EfCoreLocalization.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;

namespace fbognini.EfCoreLocalization.Persistence;

public class EfCoreLocalizationDbContext : DbContext
{
    private readonly EfCoreLocalizationSettings _localizationSettings;

    public EfCoreLocalizationDbContext(DbContextOptions<EfCoreLocalizationDbContext> options, IOptions<EfCoreLocalizationSettings> localizationOptions) : base(options)
    {
        _localizationSettings = localizationOptions.Value;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (!string.IsNullOrWhiteSpace(_localizationSettings.DefaultSchema))
        {
            modelBuilder.HasDefaultSchema(_localizationSettings.DefaultSchema);
        }

        // From .NET 8 onwards, string comparisons are case insensitive by default in EF Core.
        // We need to explicitly set case sensitive comparers for Key and ResourceKey properties.
        var caseSensitiveComparer = new ValueComparer<string>(
            (l, r) => string.Equals(l, r, StringComparison.Ordinal),
            v => v.GetHashCode(),
            v => v);


        modelBuilder.Entity<Language>(builder =>
        {
            builder.HasKey(s => s.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(5)
                .IsFixedLength();

            builder.Property(s => s.Description)
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Text>(builder =>
        {
            builder.HasKey(s => new { s.TextId, s.ResourceId });

            builder.Property(x => x.TextId)
                .HasMaxLength(100)
                .Metadata.SetValueComparer(caseSensitiveComparer);

            builder.Property(x => x.ResourceId)
                .HasMaxLength(50)
                .Metadata.SetValueComparer(caseSensitiveComparer);

            builder.Property(x => x.Description)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Translation>(builder =>
        {
            builder.HasKey(s => new { s.LanguageId, s.TextId, s.ResourceId });

            builder.Property(x => x.LanguageId)
                .HasMaxLength(5)
                .IsFixedLength();

            builder.Property(x => x.TextId)
                .HasMaxLength(100)
                .Metadata.SetValueComparer(caseSensitiveComparer);

            builder.Property(x => x.ResourceId)
                .HasMaxLength(50)
                .Metadata.SetValueComparer(caseSensitiveComparer);

            builder.Property(x => x.UpdatedOnUtc);

            builder.HasOne(x => x.Language)
                .WithMany(x => x.Translations)
                .HasForeignKey(s => new { s.LanguageId });

            builder.HasOne(x => x.Text)
                .WithMany(x => x.Translations)
                .HasForeignKey(s => new { s.TextId, s.ResourceId });
        });
    }

    internal DbSet<Language> Languages { get; set; }
    internal DbSet<Translation> Translations { get; set; }
    internal DbSet<Text> Texts { get; set; }

    public void DetachAllEntities()
    {
#if NET6_0_OR_GREATER
        this.ChangeTracker.Clear();
#else
        var entries = this.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
            entry.State = EntityState.Detached;
#endif


    }
}
