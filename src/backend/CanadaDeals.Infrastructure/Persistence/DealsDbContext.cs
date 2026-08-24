using CanadaDeals.Domain.Accounts;
using CanadaDeals.Domain.Administration;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Domain.Notifications;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace CanadaDeals.Infrastructure.Persistence;

public sealed class DealsDbContext(DbContextOptions<DealsDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<AdminAuditEvent> AdminAuditEvents => Set<AdminAuditEvent>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Retailer> Retailers => Set<Retailer>();
    public DbSet<StoreBannerProfile> StoreBannerProfiles => Set<StoreBannerProfile>();
    public DbSet<StoreBannerAsset> StoreBannerAssets => Set<StoreBannerAsset>();
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
    public DbSet<AffiliateProgram> AffiliatePrograms => Set<AffiliateProgram>();
    public DbSet<AffiliateLink> AffiliateLinks => Set<AffiliateLink>();
    public DbSet<StoreAffiliateDestination> StoreAffiliateDestinations => Set<StoreAffiliateDestination>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();
    public DbSet<RakutenAdvertiserCapability> RakutenAdvertiserCapabilities => Set<RakutenAdvertiserCapability>();
    public DbSet<RakutenSourceMapping> RakutenSourceMappings => Set<RakutenSourceMapping>();
    public DbSet<RakutenImportRun> RakutenImportRuns => Set<RakutenImportRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<AdminAuditEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt });
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(AdminAuditEvent.MaxSummaryLength).IsRequired();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

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
            entity.HasIndex(x => new { x.IsEnabled, x.Name });
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(140).IsRequired();
            entity.Property(x => x.IsEnabled).HasDefaultValue(true);
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

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProductId, x.State, x.CreatedAt });
            entity.HasIndex(x => new { x.ProductId, x.ContentHash }).IsUnique();
            entity.HasIndex(x => x.ProductId).IsUnique().HasFilter("\"State\" = 1");
            entity.Property(x => x.FileName).HasMaxLength(ProductImage.MaxFileNameLength).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Content).HasColumnType("bytea").IsRequired();
            entity.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(120);
            entity.Property(x => x.RightsEvidenceReference).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AllowedPlacements).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RetailerListing>().WithMany().HasForeignKey(x => x.SourceListingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MerchantPolicy>().WithMany().HasForeignKey(x => x.MerchantPolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Retailer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.IsEnabled).HasDefaultValue(true);
        });

        modelBuilder.Entity<StoreBannerProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.RetailerId).IsUnique();
            entity.HasIndex(x => new { x.IsEnabled, x.BannerOrder });
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Subtitle).HasMaxLength(180).IsRequired();
            entity.Property(x => x.AssetPath).HasMaxLength(300);
            entity.Property(x => x.AssetEvidenceReference).HasMaxLength(1000);
            entity.Property(x => x.AllowedPlacement).HasMaxLength(80);
            entity.HasOne(x => x.Retailer).WithMany().HasForeignKey(x => x.RetailerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoreBannerAsset>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.FileName).HasMaxLength(StoreBannerAsset.MaxFileNameLength).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(StoreBannerAsset.MaxContentTypeLength).IsRequired();
            entity.Property(x => x.Content).HasColumnType("bytea").IsRequired();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(x => x.IsEnabled).HasDefaultValue(true);
            entity.Property(x => x.VariantAttributesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ExternalIdentifiersJson).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<AffiliateProgram>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RetailerId, x.Provider }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.UpdatedAt });
            entity.HasOne(x => x.Retailer).WithMany().HasForeignKey(x => x.RetailerId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.ProviderProgramId).HasMaxLength(160);
            entity.Property(x => x.MediaPropertyId).HasMaxLength(160);
            entity.Property(x => x.ProviderLinkReference).HasMaxLength(160);
            entity.Property(x => x.DestinationDomainsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.TrackingDomainsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.RelationshipEvidenceReference).HasMaxLength(1000);
        });

        modelBuilder.Entity<AffiliateLink>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RetailerListingId, x.Status, x.RevalidateAt });
            entity.HasIndex(x => new { x.AffiliateProgramId, x.Status });
            entity.Property(x => x.TrackingUrl).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.DestinationUrl).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ProviderReference).HasMaxLength(240);
            entity.Property(x => x.FailureReason).HasMaxLength(160);
            entity.HasOne(x => x.RetailerListing).WithMany(x => x.AffiliateLinks)
                .HasForeignKey(x => x.RetailerListingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AffiliateProgram).WithMany()
                .HasForeignKey(x => x.AffiliateProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoreAffiliateDestination>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.AffiliateProgramId).IsUnique();
            entity.HasIndex(x => new { x.RetailerId, x.Status, x.RevalidateAt });
            entity.Property(x => x.TrackingUrl).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.DestinationUrl).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ProviderReference).HasMaxLength(240);
            entity.Property(x => x.FailureReason).HasMaxLength(160);
            entity.HasOne<Retailer>().WithMany().HasForeignKey(x => x.RetailerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AffiliateProgram).WithMany()
                .HasForeignKey(x => x.AffiliateProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClickEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.AffiliateLinkId, x.CreatedAt });
            entity.HasIndex(x => new { x.RetailerListingId, x.CreatedAt });
            entity.HasIndex(x => new { x.StoreAffiliateDestinationId, x.CreatedAt });
            entity.HasIndex(x => new { x.RetailerId, x.CreatedAt });
            entity.Property(x => x.Placement).HasMaxLength(40).IsRequired();
            entity.HasOne(x => x.AffiliateLink).WithMany().HasForeignKey(x => x.AffiliateLinkId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RetailerListing>().WithMany().HasForeignKey(x => x.RetailerListingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StoreAffiliateDestination).WithMany().HasForeignKey(x => x.StoreAffiliateDestinationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Retailer>().WithMany().HasForeignKey(x => x.RetailerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AffiliateProgram>().WithMany().HasForeignKey(x => x.AffiliateProgramId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_ClickEvents_Source", "(\"AffiliateLinkId\" IS NOT NULL AND \"RetailerListingId\" IS NOT NULL AND \"StoreAffiliateDestinationId\" IS NULL) OR (\"AffiliateLinkId\" IS NULL AND \"RetailerListingId\" IS NULL AND \"StoreAffiliateDestinationId\" IS NOT NULL AND \"RetailerId\" IS NOT NULL AND \"AffiliateProgramId\" IS NOT NULL)"));
        });

        modelBuilder.Entity<RakutenAdvertiserCapability>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.AdvertiserMid).IsUnique();
            entity.HasIndex(x => new { x.PartnershipStatus, x.AdvertiserStatus, x.CapabilityCheckedAt });
            entity.Property(x => x.AdvertiserMid).HasMaxLength(40).IsRequired();
            entity.Property(x => x.AdvertiserName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.AdvertiserUrl).HasMaxLength(1000);
            entity.Property(x => x.ShipsToJson).HasColumnType("jsonb").IsRequired();
            entity.HasOne(x => x.Retailer).WithMany().HasForeignKey(x => x.RetailerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MerchantPolicy).WithMany().HasForeignKey(x => x.MerchantPolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RakutenSourceMapping>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.AdvertiserMid, x.SourceListingKey }).IsUnique();
            entity.HasIndex(x => x.RetailerListingId).IsUnique();
            entity.Property(x => x.AdvertiserMid).HasMaxLength(40).IsRequired();
            entity.Property(x => x.SourceListingKey).HasMaxLength(240).IsRequired();
            entity.HasOne(x => x.RetailerListing).WithMany().HasForeignKey(x => x.RetailerListingId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RakutenImportRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.AdvertiserMid, x.StartedAt });
            entity.HasIndex(x => new { x.Status, x.StartedAt });
            entity.Property(x => x.AdvertiserMid).HasMaxLength(40).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(160);
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
