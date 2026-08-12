using CanadaDeals.Domain.Accounts;
using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Domain.Notifications;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace CanadaDeals.Infrastructure.Persistence;

public sealed class DealsDbContext(DbContextOptions<DealsDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Retailer> Retailers => Set<Retailer>();
    public DbSet<RetailerListing> RetailerListings => Set<RetailerListing>();
    public DbSet<PriceObservation> PriceObservations => Set<PriceObservation>();
    public DbSet<MerchantPolicy> MerchantPolicies => Set<MerchantPolicy>();
    public DbSet<ListingIssueReport> ListingIssueReports => Set<ListingIssueReport>();
    public DbSet<SavedProduct> SavedProducts => Set<SavedProduct>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<AccountConfirmationDelivery> AccountConfirmationDeliveries => Set<AccountConfirmationDelivery>();
    public DbSet<ControlledEmailCapture> ControlledEmailCaptures => Set<ControlledEmailCapture>();
    public DbSet<ProcessedEmailWebhook> ProcessedEmailWebhooks => Set<ProcessedEmailWebhook>();
    public DbSet<EmailSuppression> EmailSuppressions => Set<EmailSuppression>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
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
            entity.Property(x => x.SearchDocument).HasColumnType("text").IsRequired();
            entity.Property(x => x.NormalizedModelNumber).HasMaxLength(120);
            entity.Property(x => x.NormalizedManufacturerPartNumber).HasMaxLength(120);
            entity.Property<NpgsqlTsVector>("SearchVector")
                .IsGeneratedTsVectorColumn("english", [nameof(Product.SearchDocument)]);
            entity.HasIndex("SearchVector").HasMethod("GIN");
            entity.HasIndex(x => x.SearchDocument).HasMethod("GIN").HasOperators("gin_trgm_ops");
            entity.HasIndex(x => x.NormalizedModelNumber);
            entity.HasIndex(x => x.NormalizedManufacturerPartNumber);
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
            entity.HasIndex(x => x.SourceObservedAt);
            entity.HasIndex(x => x.CurrentPriceAmount);
            entity.HasIndex(x => new { x.OnlineAvailability, x.MatchState });
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

        modelBuilder.Entity<ListingIssueReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
            entity.Property(x => x.Note).HasMaxLength(ListingIssueReport.MaxNoteLength);
            entity.HasOne(x => x.RetailerListing)
                .WithMany()
                .HasForeignKey(x => x.RetailerListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SavedProduct>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.ProductId });
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceAlert>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.LastEvaluatedAt });
            entity.Property(x => x.TargetPrice).HasPrecision(12, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ConsentVersion).HasMaxLength(80).IsRequired();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_PriceAlerts_TargetPrice", "\"TargetPrice\" > 0 AND \"TargetPrice\" <= 1000000");
                table.HasCheckConstraint("CK_PriceAlerts_TargetVersion", "\"TargetVersion\" > 0");
            });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PriceAlertId, x.TargetVersion, x.PriceObservationId }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
            entity.Property(x => x.TargetPrice).HasPrecision(12, 2);
            entity.Property(x => x.QualifyingPrice).HasPrecision(12, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Channel).HasMaxLength(20).IsRequired();
            entity.Property(x => x.DestinationAddress).HasMaxLength(254).IsRequired();
            entity.Property(x => x.StatusReason).HasMaxLength(120);
            entity.Property(x => x.ProviderMessageId).HasMaxLength(160);
            entity.HasIndex(x => x.ProviderMessageId).IsUnique().HasFilter("\"ProviderMessageId\" IS NOT NULL");
            entity.HasOne(x => x.PriceAlert)
                .WithMany()
                .HasForeignKey(x => x.PriceAlertId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PriceObservation>()
                .WithMany()
                .HasForeignKey(x => x.PriceObservationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountConfirmationDelivery>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => x.ProviderMessageId).IsUnique().HasFilter("\"ProviderMessageId\" IS NOT NULL");
            entity.Property(x => x.DestinationAddress).HasMaxLength(254).IsRequired();
            entity.Property(x => x.ProviderMessageId).HasMaxLength(160);
            entity.Property(x => x.StatusReason).HasMaxLength(120);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ControlledEmailCapture>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.DestinationAddress, x.CapturedAt });
            entity.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DestinationAddress).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            entity.Property(x => x.HtmlBody).HasColumnType("text").IsRequired();
            entity.Property(x => x.TextBody).HasColumnType("text").IsRequired();
        });

        modelBuilder.Entity<ProcessedEmailWebhook>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.EventId }).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(40).IsRequired();
            entity.Property(x => x.EventId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ProviderMessageId).HasMaxLength(160);
        });

        modelBuilder.Entity<EmailSuppression>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NormalizedAddress).IsUnique();
            entity.Property(x => x.NormalizedAddress).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(80).IsRequired();
        });
    }
}
