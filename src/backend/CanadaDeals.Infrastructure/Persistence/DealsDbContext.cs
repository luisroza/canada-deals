using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Infrastructure.Persistence;

public sealed class DealsDbContext(DbContextOptions<DealsDbContext> options) : DbContext(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Retailer> Retailers => Set<Retailer>();
    public DbSet<RetailerListing> RetailerListings => Set<RetailerListing>();
    public DbSet<PriceObservation> PriceObservations => Set<PriceObservation>();
    public DbSet<MerchantPolicy> MerchantPolicies => Set<MerchantPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Gtin);
            entity.HasIndex(x => new { x.BrandId, x.ModelNumber });
            entity.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Title).HasMaxLength(240).IsRequired();
            entity.Property(x => x.VariantAttributesJson).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<Retailer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        });

        modelBuilder.Entity<MerchantPolicy>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SourceKey).IsUnique();
            entity.Property(x => x.SourceKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DisclosureText).HasMaxLength(500);
            entity.Property(x => x.AllowedComparison).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RequiredAttribution).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<RetailerListing>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RetailerId, x.ExternalListingId }).IsUnique();
            entity.HasIndex(x => x.ProductId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Retailer).WithMany().HasForeignKey(x => x.RetailerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MerchantPolicy).WithMany().HasForeignKey(x => x.MerchantPolicyId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.ExternalListingId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.OriginalTitle).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ProductUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CurrentPriceAmount).HasPrecision(12, 2);
            entity.Property(x => x.CurrentPriceCurrency).HasMaxLength(3);
            entity.Property(x => x.VariantAttributesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ExternalIdentifiersJson).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<PriceObservation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RetailerListingId, x.ObservedAt, x.SourceHash }).IsUnique();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.SourceHash).HasMaxLength(120).IsRequired();
            entity.HasOne<RetailerListing>().WithMany().HasForeignKey(x => x.RetailerListingId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
