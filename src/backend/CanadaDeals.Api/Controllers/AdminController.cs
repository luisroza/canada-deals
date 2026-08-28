using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Security;
using CanadaDeals.Api.Services;
using CanadaDeals.Domain.Administration;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Infrastructure.Affiliates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = AdminAccess.Policy)]
[EnableRateLimiting("admin")]
public sealed class AdminController(DealsDbContext db, TimeProvider clock, OwnerProvidedAffiliateLinkInspector affiliateLinkInspector, IAmazonShortLinkResolver amazonShortLinkResolver, ILogger<AdminController> logger) : ControllerBase
{
    private const string TestOnlyAttribution = "TEST_ONLY";
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex AmazonPartnerTagPattern = new("^[A-Za-z0-9][A-Za-z0-9-]{2,99}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [HttpGet("session")]
    public ActionResult<AdminSessionResponse> Session() =>
        Ok(new AdminSessionResponse(true, true, User.Identity?.Name));

    [HttpPost("affiliate-links/inspect")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminAffiliateLinkInspectionResponse>> InspectAffiliateLink(
        InspectAdminAffiliateLinkRequest request,
        CancellationToken cancellationToken)
    {
        OwnerProvidedAffiliateLinkInspection inspection;
        try { inspection = affiliateLinkInspector.Inspect(request.Url); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(request.Url), exception.Message);
            return ValidationProblem(ModelState);
        }

        if (string.Equals(inspection.TrackingHost, "amzn.to", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var destination = await amazonShortLinkResolver.ResolveAsync(request.Url, cancellationToken);
                inspection = affiliateLinkInspector.InspectResolvedShortLink(request.Url, destination);
            }
            catch (AmazonShortLinkResolutionException exception)
            {
                ModelState.AddModelError(nameof(request.Url), exception.Message);
                return ValidationProblem(ModelState);
            }
        }

        var amazonRetailerKeys = new[] { "amazon", "amazon-ca", "amazon-canada" };
        var retailer = await db.Retailers.AsNoTracking()
            .Where(candidate => amazonRetailerKeys.Contains(candidate.Key) || candidate.Name == "Amazon.ca" || candidate.Name == "Amazon Canada")
            .OrderByDescending(candidate => candidate.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);
        var warnings = inspection.Warnings.ToList();
        if (retailer is null) warnings.Add("Amazon Canada is not configured as a store yet.");
        else if (!retailer.IsEnabled) warnings.Add("The matching Amazon Canada store is currently inactive.");
        var brandCandidate = await BuildBrandCandidateAsync(inspection.ResolvedProductUrl ?? request.Url, cancellationToken);

        return Ok(new AdminAffiliateLinkInspectionResponse(
            inspection.Provider == AffiliateProviderType.AmazonCreators ? "AMAZON_CREATORS" : inspection.Provider.ToString().ToUpperInvariant(),
            inspection.HandoffMode == AffiliateHandoffMode.DirectProvider ? "DIRECT_PROVIDER" : "INTERNAL_REDIRECT",
            inspection.Status,
            inspection.TrackingHost,
            inspection.DestinationHost,
            inspection.ResolvedProductUrl,
            inspection.ExternalProductId,
            inspection.CanonicalProductUrl,
            inspection.PartnerTag,
            retailer?.Id,
            retailer?.Name,
            brandCandidate,
            clock.GetUtcNow(),
            warnings));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> Dashboard(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var offers = await db.RetailerListings.AsNoTracking()
            .Include(listing => listing.Product).ThenInclude(product => product.Brand)
            .Include(listing => listing.Product).ThenInclude(product => product.Category)
            .Include(listing => listing.Retailer)
            .Include(listing => listing.MerchantPolicy)
            .Include(listing => listing.AffiliateLinks).ThenInclude(link => link.AffiliateProgram)
            .OrderByDescending(listing => listing.FetchedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking()
            .Include(product => product.Brand)
            .Include(product => product.Category)
            .Where(product => product.Brand.IsEnabled && product.Category.IsEnabled)
            .OrderBy(product => product.Title)
            .Take(500)
            .ToListAsync(cancellationToken);
        var retailers = await db.Retailers.AsNoTracking().OrderBy(retailer => retailer.Name).ToListAsync(cancellationToken);
        var managedBrands = await db.Brands.AsNoTracking().OrderBy(brand => brand.Name)
            .Select(brand => new AdminBrandManagementResponse(
                brand.Id,
                brand.Name,
                brand.Slug,
                brand.IsEnabled,
                db.Products.Count(product => product.BrandId == brand.Id),
                brand.IsEnabled ? db.RetailerListings.Count(listing => listing.Product.BrandId == brand.Id && listing.IsEnabled &&
                    (listing.OfferValidUntil == null || listing.OfferValidUntil > now) && listing.Retailer.IsEnabled &&
                    listing.Product.Category.IsEnabled && listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                    listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution) : 0))
            .ToListAsync(cancellationToken);
        var managedCategories = await db.Categories.AsNoTracking().OrderBy(category => category.Name)
            .Select(category => new AdminCategoryManagementResponse(
                category.Id,
                category.Name,
                category.Slug,
                category.IsEnabled,
                db.Products.Count(product => product.CategoryId == category.Id),
                category.IsEnabled ? db.RetailerListings.Count(listing => listing.Product.CategoryId == category.Id && listing.IsEnabled &&
                    (listing.OfferValidUntil == null || listing.OfferValidUntil > now) && listing.Product.Brand.IsEnabled &&
                    listing.Retailer.IsEnabled && listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                    listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution) : 0))
            .ToListAsync(cancellationToken);
        var managedRetailers = await db.Retailers.AsNoTracking().OrderBy(retailer => retailer.Name)
            .Select(retailer => new AdminRetailerManagementResponse(
                retailer.Id,
                retailer.Name,
                retailer.Key,
                retailer.CountryCode,
                retailer.IsEnabled,
                db.RetailerListings.Count(listing => listing.RetailerId == retailer.Id),
                retailer.IsEnabled ? db.RetailerListings.Count(listing => listing.RetailerId == retailer.Id && listing.IsEnabled &&
                    (listing.OfferValidUntil == null || listing.OfferValidUntil > now) && listing.Product.Brand.IsEnabled &&
                    listing.Product.Category.IsEnabled && listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                    listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution) : 0,
                db.StoreBannerProfiles.Any(profile => profile.RetailerId == retailer.Id),
                db.StoreBannerProfiles.Any(profile => profile.RetailerId == retailer.Id && profile.IsEnabled),
                db.AffiliatePrograms.Count(program => program.RetailerId == retailer.Id)))
            .ToListAsync(cancellationToken);
        var profiles = await db.StoreBannerProfiles.AsNoTracking().ToDictionaryAsync(profile => profile.RetailerId, cancellationToken);
        var bannerAssetRows = await db.StoreBannerAssets.AsNoTracking()
            .OrderByDescending(asset => asset.CreatedAt)
            .Select(asset => new { asset.Id, asset.FileName, asset.ContentType, SizeBytes = asset.Content.Length, asset.CreatedAt })
            .ToListAsync(cancellationToken);
        var bannerAssets = bannerAssetRows.Select(asset => new AdminBannerAssetResponse(
            asset.Id, asset.FileName, asset.ContentType, asset.SizeBytes,
            $"{StoreBannerAsset.PublicPathPrefix}{asset.Id:D}", asset.CreatedAt)).ToList();
        var productImageRows = await db.ProductImages.AsNoTracking()
            .OrderByDescending(image => image.CreatedAt)
            .Take(200)
            .Select(image => new
            {
                image.Id, image.ProductId, ProductTitle = image.Product.Title, image.FileName, image.ContentType,
                SizeBytes = image.Content.Length, image.Width, image.Height, image.Origin, image.State,
                image.RightsEvidenceReference, image.AllowedPlacements, image.EffectiveAt, image.ExpiresAt,
                image.LastValidatedAt, image.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var productImages = productImageRows.Select(image => new AdminProductImageResponse(
            image.Id, image.ProductId, image.ProductTitle, image.FileName, image.ContentType, image.SizeBytes,
            image.Width, image.Height, $"/api/v1/admin/product-images/{image.Id:D}/content", $"{ProductImage.PublicPathPrefix}{image.Id:D}",
            image.Origin.ToString().ToUpperInvariant(), image.State.ToString().ToUpperInvariant(),
            image.RightsEvidenceReference, image.AllowedPlacements, image.EffectiveAt, image.ExpiresAt,
            image.LastValidatedAt, image.CreatedAt,
            image.State == ProductImageState.Active && (image.EffectiveAt is null || image.EffectiveAt <= now) &&
            (image.ExpiresAt is null || image.ExpiresAt > now))).ToList();
        var reports = await db.ListingIssueReports.AsNoTracking()
            .Include(report => report.RetailerListing).ThenInclude(listing => listing.Retailer)
            .OrderBy(report => report.Status)
            .ThenByDescending(report => report.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var audit = await db.AdminAuditEvents.AsNoTracking().OrderByDescending(item => item.CreatedAt).Take(30).ToListAsync(cancellationToken);

        var offerResponses = offers.Select(offer => ToOfferResponse(offer, now)).ToList();
        var publicCatalogRetailerIds = await db.RetailerListings.AsNoTracking()
            .Where(listing => listing.IsEnabled && (listing.OfferValidUntil == null || listing.OfferValidUntil > now) &&
                              listing.Retailer.IsEnabled && listing.Product.Brand.IsEnabled && listing.Product.Category.IsEnabled &&
                              listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                              listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution)
            .Select(listing => listing.RetailerId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var publicCatalogRetailerSet = publicCatalogRetailerIds.ToHashSet();
        var bannerResponses = retailers
            .Select(retailer => ToBannerResponse(retailer, profiles.GetValueOrDefault(retailer.Id), now, publicCatalogRetailerSet.Contains(retailer.Id)))
            .ToList();
        var publicPositions = bannerResponses.Where(banner => banner.IsInPublicCarousel)
            .OrderBy(banner => banner.BannerOrder).ThenBy(banner => banner.Retailer, StringComparer.OrdinalIgnoreCase)
            .Select((banner, index) => new { banner.RetailerId, Position = index + 1 })
            .ToDictionary(item => item.RetailerId, item => item.Position);
        bannerResponses = bannerResponses.Select(banner => banner with
        {
            PublicPosition = publicPositions.GetValueOrDefault(banner.RetailerId, 0) is var position && position > 0 ? position : null
        }).ToList();
        var counts = new AdminDashboardCounts(
            offerResponses.Count(offer => offer.IsPubliclyEligible),
            offerResponses.Count(offer => !offer.IsPubliclyEligible),
            bannerResponses.Count(banner => banner.IsInPublicCarousel),
            bannerResponses.Count(banner => banner.IsEnabled && (!banner.IsInPublicCarousel || banner.PublicArtworkState == "FALLBACK")),
            reports.Count(report => report.Status == ListingIssueStatus.Open));

        return Ok(new AdminDashboardResponse(
            counts,
            managedBrands.Select(brand => new AdminReferenceOption(brand.Id, brand.Slug, brand.Name, brand.IsEnabled)).ToList(),
            managedCategories.Select(category => new AdminReferenceOption(category.Id, category.Slug, category.Name, category.IsEnabled)).ToList(),
            retailers.Select(retailer => new AdminReferenceOption(retailer.Id, retailer.Key, retailer.Name, retailer.IsEnabled)).ToList(),
            products.Select(product => new AdminProductReference(
                product.Id, product.Slug, product.Title, product.BrandId, product.Brand.Name, product.CategoryId,
                product.Category.Name, product.ModelNumber, product.ManufacturerPartNumber, product.Gtin, product.VariantAttributes)).ToList(),
            managedBrands,
            managedCategories,
            managedRetailers,
            await db.MerchantPolicies.AsNoTracking().OrderBy(policy => policy.SourceKey).Select(policy => new AdminPolicyOption(
                policy.Id, policy.SourceKey, policy.AllowPriceStorage.ToString().ToUpper(), policy.AllowPriceHistory.ToString().ToUpper(),
                policy.AllowAffiliateLinks.ToString().ToUpper(), policy.RequiredAttribution)).ToListAsync(cancellationToken),
            offerResponses,
            productImages,
            bannerAssets,
            bannerResponses,
            reports.Select(report => new AdminReportResponse(
                report.Id, report.RetailerListingId, report.RetailerListing.Retailer.Name, report.RetailerListing.OriginalTitle,
                report.Reason.ToContract(), report.Note, report.Status.ToContract(), report.CreatedAt, report.UpdatedAt)).ToList(),
            audit.Select(item => new AdminAuditResponse(item.Id, item.Action, item.EntityType, item.EntityId, item.Summary, item.CreatedAt)).ToList()));
    }

    [HttpPost("brands")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBrand(CreateAdminBrandRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Slug?.Trim() ?? string.Empty;
        if (!SlugPattern.IsMatch(slug))
        {
            ModelState.AddModelError(nameof(request.Slug), "Use lowercase letters, numbers, and single hyphens.");
            return ValidationProblem(ModelState);
        }

        Brand brand;
        try { brand = Brand.Create(request.Name, slug, enabled: false); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }

        db.Brands.Add(brand);
        db.AdminAuditEvents.Add(Audit("CREATE", "Brand", brand.Id, "Created an inactive brand. Slug is immutable."));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin brand creation conflicted for slug {Slug}.", slug);
            return Conflict(new ProblemDetails { Title = "Brand already exists", Detail = "The brand slug is already in use." });
        }

        return Created($"/api/v1/admin/brands/{brand.Id}", new { brandId = brand.Id });
    }

    [HttpPut("brands/{brandId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBrand(Guid brandId, UpdateAdminBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = await db.Brands.SingleOrDefaultAsync(item => item.Id == brandId, cancellationToken);
        if (brand is null) return NotFound();
        if (brand.IsEnabled && !request.IsEnabled && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when deactivating a brand.");
            return ValidationProblem(ModelState);
        }

        var nameChanged = !string.Equals(brand.Name, request.Name?.Trim(), StringComparison.Ordinal);
        try { brand.UpdateAdministrativeName(request.Name ?? string.Empty); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }
        var statusChanged = brand.IsEnabled != request.IsEnabled;
        brand.SetEnabled(request.IsEnabled);

        if (nameChanged)
        {
            var products = await db.Products.Include(product => product.Brand).Include(product => product.Category)
                .Where(product => product.BrandId == brandId).ToListAsync(cancellationToken);
            foreach (var product in products) product.RefreshSearchDocument();
        }

        db.AdminAuditEvents.Add(Audit(statusChanged ? (request.IsEnabled ? "ACTIVATE" : "DEACTIVATE") : "UPDATE", "Brand", brand.Id,
            $"Updated brand '{brand.Slug}'. Reason: {Normalize(request.ChangeReason) ?? "Routine catalog update"}."));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin brand update conflicted for normalized identity {NormalizedKey}.", brand.NormalizedKey);
            return Conflict(new ProblemDetails { Title = "Brand already exists", Detail = "Another brand already uses this normalized name." });
        }
        return NoContent();
    }

    [HttpPost("categories")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateAdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Slug?.Trim() ?? string.Empty;
        if (!SlugPattern.IsMatch(slug))
        {
            ModelState.AddModelError(nameof(request.Slug), "Use lowercase letters, numbers, and single hyphens.");
            return ValidationProblem(ModelState);
        }

        Category category;
        try { category = Category.Create(request.Name, slug, enabled: false); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }

        db.Categories.Add(category);
        db.AdminAuditEvents.Add(Audit("CREATE", "Category", category.Id, "Created an inactive category. Slug is immutable."));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin category creation conflicted for slug {Slug}.", slug);
            return Conflict(new ProblemDetails { Title = "Category already exists", Detail = "The category slug is already in use." });
        }

        return Created($"/api/v1/admin/categories/{category.Id}", new { categoryId = category.Id });
    }

    [HttpPut("categories/{categoryId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, UpdateAdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (category is null) return NotFound();
        if (category.IsEnabled && !request.IsEnabled && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when deactivating a category.");
            return ValidationProblem(ModelState);
        }

        var nameChanged = !string.Equals(category.Name, request.Name?.Trim(), StringComparison.Ordinal);
        try { category.UpdateAdministrativeName(request.Name ?? string.Empty); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }
        var statusChanged = category.IsEnabled != request.IsEnabled;
        category.SetEnabled(request.IsEnabled);

        if (nameChanged)
        {
            var products = await db.Products.Include(product => product.Brand).Include(product => product.Category)
                .Where(product => product.CategoryId == categoryId).ToListAsync(cancellationToken);
            foreach (var product in products) product.RefreshSearchDocument();
        }

        db.AdminAuditEvents.Add(Audit(statusChanged ? (request.IsEnabled ? "ACTIVATE" : "DEACTIVATE") : "UPDATE", "Category", category.Id,
            $"Updated category '{category.Slug}'. Reason: {Normalize(request.ChangeReason) ?? "Routine catalog update"}."));
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("retailers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRetailer(CreateAdminRetailerRequest request, CancellationToken cancellationToken)
    {
        var key = request.Key?.Trim() ?? string.Empty;
        if (!SlugPattern.IsMatch(key))
        {
            ModelState.AddModelError(nameof(request.Key), "Use lowercase letters, numbers, and single hyphens.");
            return ValidationProblem(ModelState);
        }

        Retailer retailer;
        try { retailer = Retailer.Create(key, request.Name, enabled: false); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }

        db.Retailers.Add(retailer);
        db.AdminAuditEvents.Add(Audit("CREATE", "Retailer", retailer.Id, "Created an inactive Canadian store. Store key is immutable."));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin retailer creation conflicted for key {Key}.", key);
            return Conflict(new ProblemDetails { Title = "Store already exists", Detail = "The store key is already in use." });
        }

        return Created($"/api/v1/admin/retailers/{retailer.Id}", new { retailerId = retailer.Id });
    }

    [HttpPut("retailers/{retailerId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRetailer(Guid retailerId, UpdateAdminRetailerRequest request, CancellationToken cancellationToken)
    {
        var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Id == retailerId, cancellationToken);
        if (retailer is null) return NotFound();
        if (retailer.IsEnabled && !request.IsEnabled && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when deactivating a store.");
            return ValidationProblem(ModelState);
        }

        try { retailer.UpdateAdministrativeName(request.Name ?? string.Empty); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.Name), exception.Message);
            return ValidationProblem(ModelState);
        }
        var statusChanged = retailer.IsEnabled != request.IsEnabled;
        retailer.SetEnabled(request.IsEnabled);
        db.AdminAuditEvents.Add(Audit(statusChanged ? (request.IsEnabled ? "ACTIVATE" : "DEACTIVATE") : "UPDATE", "Retailer", retailer.Id,
            $"Updated store '{retailer.Key}'. Reason: {Normalize(request.ChangeReason) ?? "Routine store update"}."));
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("reports/{reportId:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReportStatus(Guid reportId, UpdateAdminReportRequest request, CancellationToken cancellationToken)
    {
        var report = await db.ListingIssueReports.SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report is null) return NotFound();
        if (!ListingIssueReportContractValues.TryParseStatus(request.Status, out var status))
        {
            ModelState.AddModelError(nameof(request.Status), "Choose OPEN, REVIEWED, RESOLVED, or DISMISSED.");
            return ValidationProblem(ModelState);
        }

        var previous = report.Status;
        report.ChangeStatus(status, clock.GetUtcNow());
        db.AdminAuditEvents.Add(Audit("STATUS_CHANGE", "ListingIssueReport", report.Id,
            $"Changed report status from {previous.ToContract()} to {status.ToContract()}. Resolution: {request.ResolutionNote.Trim()}."));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} changed Report {ReportId} from {PreviousStatus} to {Status}.", ActorId(), report.Id, previous, status);
        return NoContent();
    }

    [HttpPost("offers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOffer(UpsertAdminOfferRequest request, CancellationToken cancellationToken)
    {
        var context = await ValidateOfferAsync(request, null, cancellationToken);
        if (context is null) return ValidationProblem(ModelState);

        var product = context.Product ?? Product.Create(
            request.Slug.Trim(), request.ProductTitle.Trim(), context.Brand, context.Category,
            Normalize(request.ModelNumber), Normalize(request.ManufacturerPartNumber), Normalize(request.Gtin), request.VariantAttributes);
        var listing = RetailerListing.Create(
            product.Id, context.Retailer.Id, request.ExternalListingId.Trim(), request.OriginalTitle.Trim(), request.ProductUrl.Trim(),
            context.Policy.Id, context.MatchState, request.ObservedAt, request.FetchedAt, request.CurrentPrice, "CAD", FreshnessState.Recent,
            context.Policy.CanPublishCurrentPrice ? EvidenceState.Partial : EvidenceState.Unavailable, HistoryAvailability.Unavailable,
            request.VariantAttributes, request.ExternalIdentifiers, Normalize(request.RetailerSku), context.OwnerProvidedLink?.DestinationUrl ?? Normalize(request.ApprovedAffiliateDestinationReference),
            Normalize(request.Seller), request.IsMarketplaceSeller, context.Condition, request.PackQuantity, Normalize(request.BundleContents),
            Normalize(request.RegionAvailabilityContext), context.Availability, Normalize(request.ShippingContext), request.OfferValidUntil);
        listing.SetEnabled(request.IsEnabled);

        if (context.BrandWasCreated) db.Brands.Add(context.Brand);
        if (context.Product is null) db.Products.Add(product);
        db.RetailerListings.Add(listing);
        SyncOwnerProvidedAffiliateLink(listing, context.OwnerProvidedLink, request.ChangeReason);
        if (context.BrandWasCreated)
            db.AdminAuditEvents.Add(Audit("CREATE", "Brand", context.Brand.Id,
                $"Created and activated brand '{context.Brand.Slug}' after explicit confirmation while saving an offer."));
        else if (context.BrandWasActivated)
            db.AdminAuditEvents.Add(Audit("ACTIVATE", "Brand", context.Brand.Id,
                $"Reactivated existing brand '{context.Brand.Slug}' after explicit confirmation while saving an offer."));
        db.AdminAuditEvents.Add(Audit("CREATE", "RetailerListing", listing.Id,
            context.Product is null
                ? request.IsEnabled ? "Created a new Product and published its administrative offer." : "Created a new Product with an administrative offer draft."
                : request.IsEnabled ? "Added and published an offer for an existing Product." : "Added an offer draft for an existing Product."));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin offer creation conflicted with an existing record.");
            return Conflict(new ProblemDetails { Title = "Offer or brand already exists", Detail = "The Product, retailer listing, or normalized brand identity is already in use. Validate the link again to reuse the existing record." });
        }

        logger.LogInformation("Owner admin {UserId} created Listing {ListingId}.", ActorId(), listing.Id);
        return Created($"/api/v1/admin/offers/{listing.Id}", new { listingId = listing.Id, productId = product.Id, brandId = context.Brand.Id, previewPath = $"/products/{product.Slug}" });
    }

    [HttpPut("offers/{listingId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOffer(Guid listingId, UpsertAdminOfferRequest request, CancellationToken cancellationToken)
    {
        var listing = await db.RetailerListings
            .Include(item => item.Product).ThenInclude(product => product.Brand)
            .Include(item => item.Product).ThenInclude(product => product.Category)
            .Include(item => item.Retailer)
            .Include(item => item.MerchantPolicy)
            .Include(item => item.AffiliateLinks).ThenInclude(link => link.AffiliateProgram)
            .SingleOrDefaultAsync(item => item.Id == listingId, cancellationToken);
        if (listing is null) return NotFound();

        var context = await ValidateOfferAsync(request, listing, cancellationToken);
        if (context is null) return ValidationProblem(ModelState);
        if ((listing.IsEnabled && !request.IsEnabled || listing.MatchState != context.MatchState) && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when deactivating an offer or changing its match decision.");
            return ValidationProblem(ModelState);
        }

        listing.Product.UpdateAdministrativeIdentity(
            request.ProductTitle.Trim(), context.Brand, context.Category,
            Normalize(request.ModelNumber), Normalize(request.ManufacturerPartNumber), Normalize(request.Gtin), request.VariantAttributes);
        listing.UpdateAdministrativeDetails(
            request.OriginalTitle, request.ProductUrl, request.CurrentPrice, request.ObservedAt, request.RetailerSku,
            context.OwnerProvidedLink?.DestinationUrl ?? request.ApprovedAffiliateDestinationReference, request.Seller, request.IsMarketplaceSeller, context.Condition,
            request.PackQuantity, request.BundleContents, request.VariantAttributes, request.ExternalIdentifiers, context.Availability,
            request.RegionAvailabilityContext, request.ShippingContext, request.IsEnabled, request.FetchedAt, request.OfferValidUntil);
        listing.SetAdministrativeMatchState(context.MatchState);
        SyncOwnerProvidedAffiliateLink(listing, context.OwnerProvidedLink, request.ChangeReason);
        db.AdminAuditEvents.Add(Audit("UPDATE", "RetailerListing", listing.Id,
            $"Updated administrative offer. Reason: {Normalize(request.ChangeReason) ?? "Routine editorial update"}."));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Owner admin offer update conflicted for Listing {ListingId}.", listingId);
            return Conflict(new ProblemDetails { Title = "Offer update conflict", Detail = "The product slug or retailer listing ID conflicts with an existing record." });
        }

        logger.LogInformation("Owner admin {UserId} updated Listing {ListingId}.", ActorId(), listing.Id);
        return NoContent();
    }

    [HttpPut("banners/selection")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBannerSelection(UpdateAdminBannerSelectionRequest request, CancellationToken cancellationToken)
    {
        var activeRetailerIds = (request.ActiveRetailerIds ?? []).Distinct().ToHashSet();
        var profiles = await db.StoreBannerProfiles.ToListAsync(cancellationToken);
        var profileRetailerIds = profiles.Select(profile => profile.RetailerId).ToHashSet();
        if (!activeRetailerIds.IsSubsetOf(profileRetailerIds))
        {
            ModelState.AddModelError(nameof(request.ActiveRetailerIds), "Configure a banner before activating it in the homepage carousel.");
            return ValidationProblem(ModelState);
        }

        var changes = profiles.Where(profile => profile.IsEnabled != activeRetailerIds.Contains(profile.RetailerId)).ToList();
        if (changes.Any(profile => profile.IsEnabled) && string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when removing banners from the homepage carousel.");
            return ValidationProblem(ModelState);
        }

        foreach (var profile in changes)
        {
            var enabled = activeRetailerIds.Contains(profile.RetailerId);
            profile.SetEnabled(enabled);
            db.AdminAuditEvents.Add(Audit(enabled ? "ACTIVATE" : "DEACTIVATE", "StoreBannerProfile", profile.Id,
                $"{(enabled ? "Added banner to" : "Removed banner from")} the homepage carousel. Reason: {Normalize(request.ChangeReason) ?? "Carousel selection update"}."));
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} changed {Count} homepage carousel selections.", ActorId(), changes.Count);
        return NoContent();
    }

    [HttpPut("banners/{retailerId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertBanner(Guid retailerId, UpsertAdminBannerRequest request, CancellationToken cancellationToken)
    {
        var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Id == retailerId, cancellationToken);
        if (retailer is null) return NotFound();
        if (request.AssetPath?.StartsWith(StoreBannerAsset.PublicPathPrefix, StringComparison.Ordinal) == true)
        {
            var idText = request.AssetPath[StoreBannerAsset.PublicPathPrefix.Length..];
            if (!Guid.TryParse(idText, out var assetId) ||
                !await db.StoreBannerAssets.AsNoTracking().AnyAsync(asset => asset.Id == assetId, cancellationToken))
            {
                ModelState.AddModelError(nameof(request.AssetPath), "Choose an uploaded reviewed banner asset.");
                return ValidationProblem(ModelState);
            }
        }
        var profile = await db.StoreBannerProfiles.SingleOrDefaultAsync(item => item.RetailerId == retailerId, cancellationToken);
        var isNewProfile = profile is null;
        var carouselSelected = profile?.IsEnabled ?? false;

        try
        {
            if (string.Equals(request.AssetSource, "CANADADEALSORIGINAL", StringComparison.OrdinalIgnoreCase))
            {
                if (profile is null)
                {
                    profile = StoreBannerProfile.CreateOriginal(retailerId, request.Title, request.Subtitle, request.AssetPath, request.BannerOrder, carouselSelected);
                    db.StoreBannerProfiles.Add(profile);
                }
                else profile.UpdateOriginal(request.Title, request.Subtitle, request.AssetPath, request.BannerOrder, carouselSelected);
            }
            else if (string.Equals(request.AssetSource, "MERCHANTAPPROVEDAFFILIATEASSET", StringComparison.OrdinalIgnoreCase) &&
                     Enum.TryParse<AffiliateProviderType>(request.AssetProvider, true, out var provider) && provider != AffiliateProviderType.Unknown &&
                     request.EffectiveAt.HasValue)
            {
                if (profile is null)
                {
                    profile = StoreBannerProfile.CreateMerchantApproved(retailerId, provider, request.Title, request.Subtitle, request.AssetPath ?? string.Empty,
                        request.BannerOrder, request.AssetEvidenceReference ?? string.Empty, request.AllowedPlacement ?? string.Empty, request.EffectiveAt.Value,
                        request.ExpiresAt, carouselSelected);
                    db.StoreBannerProfiles.Add(profile);
                }
                else profile.UpdateMerchantApproved(provider, request.Title, request.Subtitle, request.AssetPath ?? string.Empty,
                    request.BannerOrder, request.AssetEvidenceReference ?? string.Empty, request.AllowedPlacement ?? string.Empty, request.EffectiveAt.Value,
                    request.ExpiresAt, carouselSelected);
            }
            else
            {
                ModelState.AddModelError(nameof(request.AssetSource), "Choose CANADADEALSORIGINAL or provide every required field for MERCHANTAPPROVEDAFFILIATEASSET.");
                return ValidationProblem(ModelState);
            }
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(request.AssetPath), exception.Message);
            return ValidationProblem(ModelState);
        }

        db.AdminAuditEvents.Add(Audit(isNewProfile ? "CREATE" : "UPDATE", "StoreBannerProfile", profile.Id,
            $"Updated the banner for Retailer {retailer.Id}. Reason: {Normalize(request.ChangeReason) ?? "Routine editorial update"}."));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} updated StoreBannerProfile {ProfileId}.", ActorId(), profile.Id);
        return NoContent();
    }

    [HttpPost("banner-assets")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(StoreBannerAsset.MaxBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = StoreBannerAsset.MaxBytes + 65536)]
    public async Task<ActionResult<AdminBannerAssetResponse>> UploadBannerAsset(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length is <= 0 or > StoreBannerAsset.MaxBytes)
        {
            ModelState.AddModelError(nameof(file), "Choose a PNG, JPEG, or WebP image no larger than 2 MB.");
            return ValidationProblem(ModelState);
        }

        var fileName = Path.GetFileName(file.FileName).Trim();
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > StoreBannerAsset.MaxFileNameLength)
        {
            ModelState.AddModelError(nameof(file), $"Use a file name of at most {StoreBannerAsset.MaxFileNameLength} characters.");
            return ValidationProblem(ModelState);
        }

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var bytes = stream.ToArray();
        var contentType = file.ContentType.Trim().ToLowerInvariant();
        if (!IsSupportedBannerImage(contentType, bytes))
        {
            ModelState.AddModelError(nameof(file), "The uploaded bytes must be a valid PNG, JPEG, or WebP image.");
            return ValidationProblem(ModelState);
        }

        var asset = StoreBannerAsset.Create(fileName, contentType, bytes, ActorId(), clock.GetUtcNow());
        db.StoreBannerAssets.Add(asset);
        db.AdminAuditEvents.Add(Audit("UPLOAD", "StoreBannerAsset", asset.Id,
            $"Uploaded reviewed banner image {asset.FileName} ({asset.ContentType}, {asset.Content.Length} bytes)."));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} uploaded StoreBannerAsset {AssetId}.", ActorId(), asset.Id);

        var response = new AdminBannerAssetResponse(asset.Id, asset.FileName, asset.ContentType, asset.Content.Length, asset.PublicPath, asset.CreatedAt);
        return Created(asset.PublicPath, response);
    }

    [HttpPost("products/{productId:guid}/images")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(ProductImage.MaxBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductImage.MaxBytes + 65536)]
    public async Task<ActionResult<AdminProductImageResponse>> UploadProductImage(
        Guid productId,
        [FromForm] UploadAdminProductImageRequest request,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null) return NotFound();
        if (request.File is null || request.File.Length is <= 0 or > ProductImage.MaxBytes)
        {
            ModelState.AddModelError(nameof(request.File), "Choose a PNG, JPEG, or WebP image no larger than 1 MB.");
            return ValidationProblem(ModelState);
        }

        var fileName = Path.GetFileName(request.File.FileName).Trim();
        await using var stream = new MemoryStream((int)request.File.Length);
        await request.File.CopyToAsync(stream, cancellationToken);
        var bytes = stream.ToArray();
        if (!ProductImageFileInspector.TryInspect(request.File.ContentType, bytes, out var inspection) || inspection is null)
        {
            ModelState.AddModelError(nameof(request.File), "The file signature and dimensions must identify a valid PNG, JPEG, or WebP image.");
            return ValidationProblem(ModelState);
        }
        if (inspection.Width > ProductImage.MaxDimension || inspection.Height > ProductImage.MaxDimension)
        {
            ModelState.AddModelError(nameof(request.File), $"Use an image no larger than {ProductImage.MaxDimension} by {ProductImage.MaxDimension} pixels.");
            return ValidationProblem(ModelState);
        }
        if (await db.ProductImages.AnyAsync(image => image.ProductId == productId && image.ContentHash == inspection.Sha256, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Image already exists", Detail = "This exact image is already registered for the product." });

        ProductImage image;
        try
        {
            image = ProductImage.CreateOwnerReviewed(
                productId, fileName, inspection.ContentType, bytes, inspection.Width, inspection.Height, inspection.Sha256,
                request.RightsEvidenceReference, request.AllowedPlacements, request.EffectiveAt, request.ExpiresAt,
                ActorId(), clock.GetUtcNow(), request.Activate);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.File), exception.Message);
            return ValidationProblem(ModelState);
        }

        if (request.Activate)
        {
            var current = await db.ProductImages.Where(item => item.ProductId == productId && item.State == ProductImageState.Active)
                .ToListAsync(cancellationToken);
            foreach (var existing in current) existing.Archive(clock.GetUtcNow());
        }
        db.ProductImages.Add(image);
        db.AdminAuditEvents.Add(Audit(request.Activate ? "UPLOAD_AND_ACTIVATE" : "UPLOAD", "ProductImage", image.Id,
            $"Uploaded owner-reviewed product image for Product {productId}. Placements: {image.AllowedPlacements}."));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Owner admin {UserId} uploaded ProductImage {ImageId} for Product {ProductId}.", ActorId(), image.Id, productId);
        return Created(image.PublicPath, ToProductImageResponse(image, product.Title, clock.GetUtcNow()));
    }

    [HttpGet("product-images/{imageId:guid}/content")]
    public async Task<IActionResult> PreviewProductImage(Guid imageId, CancellationToken cancellationToken)
    {
        var image = await db.ProductImages.AsNoTracking().SingleOrDefaultAsync(item => item.Id == imageId, cancellationToken);
        if (image is null) return NotFound();
        Response.Headers.CacheControl = "private,no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Content, image.ContentType);
    }

    [HttpPost("product-images/{imageId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateProductImage(Guid imageId, UpdateAdminProductImageStateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when activating a product image.");
            return ValidationProblem(ModelState);
        }
        var image = await db.ProductImages.SingleOrDefaultAsync(item => item.Id == imageId, cancellationToken);
        if (image is null) return NotFound();
        var now = clock.GetUtcNow();
        try { image.Activate(now); }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(imageId), exception.Message);
            return ValidationProblem(ModelState);
        }
        var current = await db.ProductImages.Where(item => item.ProductId == image.ProductId && item.Id != image.Id && item.State == ProductImageState.Active)
            .ToListAsync(cancellationToken);
        foreach (var existing in current) existing.Archive(now);
        db.AdminAuditEvents.Add(Audit("ACTIVATE", "ProductImage", image.Id, $"Activated product image. Reason: {request.ChangeReason.Trim()}."));
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("product-images/{imageId:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveProductImage(Guid imageId, UpdateAdminProductImageStateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when archiving a product image.");
            return ValidationProblem(ModelState);
        }
        var image = await db.ProductImages.SingleOrDefaultAsync(item => item.Id == imageId, cancellationToken);
        if (image is null) return NotFound();
        image.Archive(clock.GetUtcNow());
        db.AdminAuditEvents.Add(Audit("ARCHIVE", "ProductImage", image.Id, $"Archived product image. Reason: {request.ChangeReason.Trim()}."));
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<OfferValidationContext?> ValidateOfferAsync(UpsertAdminOfferRequest request, RetailerListing? existing, CancellationToken cancellationToken)
    {
        var requestedSlug = (request.Slug ?? string.Empty).Trim();
        if (!SlugPattern.IsMatch(requestedSlug)) ModelState.AddModelError(nameof(request.Slug), "Use lowercase letters, numbers, and single hyphens.");
        ValidateHttpsUrl(request.ProductUrl, nameof(request.ProductUrl), required: true);
        ValidateHttpsUrl(request.ApprovedAffiliateDestinationReference, nameof(request.ApprovedAffiliateDestinationReference), required: false);
        ValidateDictionary(request.VariantAttributes, nameof(request.VariantAttributes));
        ValidateDictionary(request.ExternalIdentifiers, nameof(request.ExternalIdentifiers));
        var now = clock.GetUtcNow();
        if (request.ObservedAt > request.FetchedAt) ModelState.AddModelError(nameof(request.ObservedAt), "Observed time cannot be later than fetched time.");
        if (request.FetchedAt > now.AddMinutes(5)) ModelState.AddModelError(nameof(request.FetchedAt), "Fetched time cannot be in the future.");
        if (request.OfferValidUntil.HasValue && request.OfferValidUntil <= request.ObservedAt)
            ModelState.AddModelError(nameof(request.OfferValidUntil), "Offer validity must end after the observed time.");
        if (request.IsEnabled && request.OfferValidUntil.HasValue && request.OfferValidUntil <= now)
            ModelState.AddModelError(nameof(request.OfferValidUntil), "An expired offer cannot be published. Save it as a draft or choose a future validity time.");
        if (decimal.Round(request.CurrentPrice, 2) != request.CurrentPrice) ModelState.AddModelError(nameof(request.CurrentPrice), "Price supports at most two decimal places.");

        if (!Enum.TryParse<ProductCondition>(request.ConditionState, true, out var condition) || !Enum.IsDefined(condition))
            ModelState.AddModelError(nameof(request.ConditionState), "Choose NEW, USED, REFURBISHED, or UNKNOWN.");
        if (!Enum.TryParse<OnlineAvailabilityState>(request.AvailabilityState, true, out var availability) || !Enum.IsDefined(availability))
            ModelState.AddModelError(nameof(request.AvailabilityState), "Choose AVAILABLE, UNAVAILABLE, or UNKNOWN.");
        if (!Enum.TryParse<MatchState>(request.MatchState, true, out var matchState) || !Enum.IsDefined(matchState))
            ModelState.AddModelError(nameof(request.MatchState), "Choose CONFIRMED, MANUALREVIEW, POSSIBLEMATCHREVIEW, or NOMATCH.");

        Product? product = existing?.Product;
        if (existing is null && request.ProductId.HasValue)
        {
            product = await db.Products.Include(item => item.Brand).Include(item => item.Category)
                .SingleOrDefaultAsync(item => item.Id == request.ProductId.Value, cancellationToken);
            if (product is null) ModelState.AddModelError(nameof(request.ProductId), "Choose an existing Product or create a new one.");
        }

        // Reusing a Product locks its identity. Editing an existing offer may still update
        // the Product's mutable identity fields while preserving its immutable slug.
        var reusedProduct = existing is null ? product : null;
        var brandResolution = await ResolveOfferBrandAsync(request, existing, reusedProduct, cancellationToken);
        var brand = brandResolution?.Brand;
        var category = reusedProduct?.Category ?? await db.Categories.SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken);
        var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Id == request.RetailerId, cancellationToken);
        var policy = await db.MerchantPolicies.SingleOrDefaultAsync(item => item.Id == request.MerchantPolicyId, cancellationToken);
        if (category is null || !category.IsEnabled) ModelState.AddModelError(nameof(request.CategoryId), "Choose an enabled category.");
        if (retailer is null || !retailer.IsEnabled) ModelState.AddModelError(nameof(request.RetailerId), "Choose an enabled retailer.");
        if (policy is null) ModelState.AddModelError(nameof(request.MerchantPolicyId), "Choose an existing merchant policy.");
        if (existing is not null)
        {
            if (request.ProductId.HasValue && existing.ProductId != request.ProductId.Value) ModelState.AddModelError(nameof(request.ProductId), "Product cannot be changed after creation.");
            if (!string.Equals(existing.Product.Slug, requestedSlug, StringComparison.Ordinal)) ModelState.AddModelError(nameof(request.Slug), "Product slug cannot be changed after creation.");
            if (existing.RetailerId != request.RetailerId) ModelState.AddModelError(nameof(request.RetailerId), "Retailer cannot be changed after creation.");
            if (existing.MerchantPolicyId != request.MerchantPolicyId) ModelState.AddModelError(nameof(request.MerchantPolicyId), "Merchant policy cannot be changed after creation.");
            if (!string.Equals(existing.ExternalListingId, request.ExternalListingId.Trim(), StringComparison.Ordinal)) ModelState.AddModelError(nameof(request.ExternalListingId), "External listing ID cannot be changed after creation.");
        }
        else if (product is not null)
        {
            if (!string.Equals(product.Slug, requestedSlug, StringComparison.Ordinal) || product.BrandId != brand?.Id || product.CategoryId != request.CategoryId)
                ModelState.AddModelError(nameof(request.ProductId), "The selected Product identity must be used without changing its slug, brand, or category.");
        }
        else if (await db.Products.AnyAsync(item => item.Slug == requestedSlug, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Slug), "This Product slug already exists. Add the offer to the existing Product instead.");
        }
        if (request.IsEnabled && policy is not null && (!policy.CanPublishCurrentPrice || string.Equals(policy.RequiredAttribution, TestOnlyAttribution, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(request.IsEnabled), "This merchant policy does not permit a public current-price offer. Save it as a draft.");
        if (!string.IsNullOrWhiteSpace(request.ApprovedAffiliateDestinationReference) && policy?.CanUseAffiliateLinks != true)
            ModelState.AddModelError(nameof(request.ApprovedAffiliateDestinationReference), "The selected merchant policy does not permit affiliate links.");

        OwnerProvidedAffiliateLinkPlan? ownerProvidedLink = null;
        if (retailer is not null && policy is not null)
            ownerProvidedLink = await ValidateOwnerProvidedAffiliateLinkAsync(request, retailer, policy, existing, cancellationToken);

        return ModelState.IsValid && brandResolution is not null && brand is not null && category is not null && retailer is not null && policy is not null
            ? new OfferValidationContext(product, brand, brandResolution.WasCreated, brandResolution.WasActivated, category, retailer, policy, condition, availability, matchState, ownerProvidedLink)
            : null;
    }

    private async Task<BrandResolution?> ResolveOfferBrandAsync(
        UpsertAdminOfferRequest request,
        RetailerListing? existing,
        Product? reusedProduct,
        CancellationToken cancellationToken)
    {
        if (reusedProduct is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.NewBrandName) || request.ConfirmBrandCreation)
                ModelState.AddModelError(nameof(request.NewBrandName), "An existing Product keeps its canonical brand.");
            return new BrandResolution(reusedProduct.Brand, false, false);
        }

        if (request.BrandId.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(request.NewBrandName) || request.ConfirmBrandCreation)
                ModelState.AddModelError(nameof(request.NewBrandName), "Choose an existing brand or confirm a new brand, not both.");
            var selected = await db.Brands.SingleOrDefaultAsync(item => item.Id == request.BrandId.Value, cancellationToken);
            if (selected is null || !selected.IsEnabled)
            {
                ModelState.AddModelError(nameof(request.BrandId), "Choose an enabled brand.");
                return null;
            }
            return new BrandResolution(selected, false, false);
        }

        if (existing is not null)
        {
            ModelState.AddModelError(nameof(request.BrandId), "Choose the existing Product brand.");
            return null;
        }

        var name = Normalize(request.NewBrandName);
        var slug = Normalize(request.NewBrandSlug);
        if (name is null || slug is null)
        {
            ModelState.AddModelError(nameof(request.BrandId), "Select an existing brand or review a proposed new brand.");
            return null;
        }
        if (!request.ConfirmBrandCreation)
        {
            ModelState.AddModelError(nameof(request.ConfirmBrandCreation), "Confirm the proposed brand before saving the offer.");
            return null;
        }
        if (!SlugPattern.IsMatch(slug))
        {
            ModelState.AddModelError(nameof(request.NewBrandSlug), "Use lowercase letters, numbers, and single hyphens for the brand slug.");
            return null;
        }

        Brand proposed;
        try { proposed = Brand.Create(name, slug, enabled: true); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(exception.ParamName ?? nameof(request.NewBrandName), exception.Message);
            return null;
        }

        var matching = await db.Brands.SingleOrDefaultAsync(item => item.NormalizedKey == proposed.NormalizedKey, cancellationToken);
        if (matching is null) return new BrandResolution(proposed, true, false);
        if (matching.IsEnabled) return new BrandResolution(matching, false, false);
        matching.SetEnabled(true);
        return new BrandResolution(matching, false, true);
    }

    private async Task<OwnerProvidedAffiliateLinkPlan?> ValidateOwnerProvidedAffiliateLinkAsync(
        UpsertAdminOfferRequest request,
        Retailer retailer,
        MerchantPolicy policy,
        RetailerListing? existing,
        CancellationToken cancellationToken)
    {
        var existingOwnerLink = existing?.AffiliateLinks
            .Where(link => link.AcquisitionMode == AffiliateLinkAcquisitionMode.OwnerProvided)
            .OrderByDescending(link => link.CreatedAt)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(request.AffiliateTrackingUrl))
        {
            if (existingOwnerLink is not null && string.IsNullOrWhiteSpace(request.ChangeReason))
                ModelState.AddModelError(nameof(request.ChangeReason), "A reason is required when removing an owner-provided retailer link.");
            return null;
        }

        OwnerProvidedAffiliateLinkInspection inspection;
        try { inspection = affiliateLinkInspector.Inspect(request.AffiliateTrackingUrl); }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(request.AffiliateTrackingUrl), exception.Message);
            return null;
        }

        var amazonStore = retailer.Key is "amazon" or "amazon-ca" or "amazon-canada" ||
                          retailer.Name.Equals("Amazon.ca", StringComparison.OrdinalIgnoreCase) ||
                          retailer.Name.Equals("Amazon Canada", StringComparison.OrdinalIgnoreCase);
        if (!amazonStore)
            ModelState.AddModelError(nameof(request.RetailerId), "An Amazon owner-provided link must be associated with the Amazon Canada store.");

        var asin = OwnerProvidedAffiliateLinkInspector.AmazonAsin(request.ProductUrl);
        if (asin is null)
            ModelState.AddModelError(nameof(request.ProductUrl), "Use the canonical Amazon.ca Product page containing /dp/{ASIN}.");
        else if (!string.Equals(request.ExternalListingId?.Trim(), asin, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(request.ExternalListingId), "For an Amazon link, the external listing ID must be the 10-character ASIN from the Product page.");

        var partnerTag = request.AffiliatePartnerTag?.Trim();
        if (string.IsNullOrWhiteSpace(partnerTag) || !AmazonPartnerTagPattern.IsMatch(partnerTag))
            ModelState.AddModelError(nameof(request.AffiliatePartnerTag), "Enter the Canadian Amazon Partner Tag associated with this link.");
        else if (!string.Equals(inspection.TrackingHost, "amzn.to", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(inspection.PartnerTag))
            ModelState.AddModelError(nameof(request.AffiliateTrackingUrl), "This Amazon URL is not tagged as an affiliate link. Generate the finished link in Amazon Associates/SiteStripe instead of entering a Partner Tag separately.");
        else if (!string.IsNullOrWhiteSpace(inspection.PartnerTag) && !string.Equals(inspection.PartnerTag, partnerTag, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(request.AffiliatePartnerTag), "The Partner Tag does not match the tag visible in the Amazon link.");

        if (!request.AffiliateRelationshipConfirmed)
            ModelState.AddModelError(nameof(request.AffiliateRelationshipConfirmed), "Confirm that the link belongs to the approved account and GreatDeals.ca is registered for its use.");
        var evidence = request.AffiliateRelationshipEvidenceReference?.Trim();
        if (string.IsNullOrWhiteSpace(evidence))
            ModelState.AddModelError(nameof(request.AffiliateRelationshipEvidenceReference), "Add a redacted relationship evidence reference.");
        if (!policy.CanUseAffiliateLinks)
            ModelState.AddModelError(nameof(request.MerchantPolicyId), "The selected merchant policy does not permit retailer links.");

        if (request.IsEnabled)
        {
            if (!policy.SourceKey.StartsWith("amazon-creators", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(request.IsEnabled), "Amazon offers require a verified amazon-creators Merchant Policy before publication. Save this offer as a draft.");
            var disclosure = $"{policy.RequiredAttribution} {policy.DisclosureText}";
            if (!disclosure.Contains("Amazon Associate", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(request.IsEnabled), "The Amazon Merchant Policy must include the required Amazon Associate disclosure before publication.");
        }

        var program = await db.AffiliatePrograms
            .SingleOrDefaultAsync(candidate => candidate.RetailerId == retailer.Id && candidate.Provider == AffiliateProviderType.AmazonCreators, cancellationToken);
        if (program?.Status == AffiliateProgramStatus.Active && !string.Equals(program.ProviderProgramId, partnerTag, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(request.AffiliatePartnerTag), "This store already has a different active Amazon Partner Tag. Review the program configuration instead of replacing it from an offer.");

        return ModelState.IsValid && asin is not null && partnerTag is not null && evidence is not null
            ? new OwnerProvidedAffiliateLinkPlan(request.AffiliateTrackingUrl, request.ProductUrl.Trim(), asin, partnerTag, evidence, program)
            : null;
    }

    private void SyncOwnerProvidedAffiliateLink(RetailerListing listing, OwnerProvidedAffiliateLinkPlan? plan, string? reason)
    {
        var existingOwnerLinks = listing.AffiliateLinks
            .Where(link => link.AcquisitionMode == AffiliateLinkAcquisitionMode.OwnerProvided)
            .ToList();
        if (plan is null)
        {
            foreach (var link in existingOwnerLinks.Where(link => link.Status != AffiliateLinkStatus.Disabled))
                link.Disable(Normalize(reason) ?? "OWNER_REMOVED");
            return;
        }

        var now = clock.GetUtcNow();
        var program = plan.Program;
        if (program is null)
        {
            program = AffiliateProgram.Create(
                listing.RetailerId,
                AffiliateProviderType.AmazonCreators,
                AffiliateProgramStatus.Active,
                now,
                providerProgramId: plan.PartnerTag,
                allowsDeepLinking: true,
                destinationDomains: ["amazon.ca"],
                trackingDomains: ["amzn.to", "amazon.ca"],
                relationshipEvidenceReference: plan.RelationshipEvidenceReference,
                relationshipValidatedAt: now);
            db.AffiliatePrograms.Add(program);
        }
        else if (program.Status != AffiliateProgramStatus.Active)
        {
            program.ActivateOwnerProvidedAmazon(
                plan.PartnerTag,
                ["amazon.ca"],
                ["amzn.to", "amazon.ca"],
                plan.RelationshipEvidenceReference,
                now);
        }

        var unchanged = existingOwnerLinks.FirstOrDefault(link =>
            link.Status == AffiliateLinkStatus.Active &&
            link.Provider == AffiliateProviderType.AmazonCreators &&
            link.HandoffMode == AffiliateHandoffMode.DirectProvider &&
            string.Equals(link.TrackingUrl, plan.TrackingUrl, StringComparison.Ordinal) &&
            string.Equals(link.DestinationUrl, plan.DestinationUrl, StringComparison.Ordinal));
        foreach (var link in existingOwnerLinks.Where(link => link != unchanged && link.Status != AffiliateLinkStatus.Disabled))
            link.Disable("OWNER_REPLACED");
        if (unchanged is not null) return;

        db.AffiliateLinks.Add(AffiliateLink.CreateOwnerProvidedActive(
            listing.Id,
            program.Id,
            AffiliateProviderType.AmazonCreators,
            plan.TrackingUrl,
            plan.DestinationUrl,
            now,
            now.AddDays(30),
            providerReference: "OWNER_PROVIDED_AMAZON_LINK"));
    }

    private AdminOfferResponse ToOfferResponse(RetailerListing listing, DateTimeOffset now)
    {
        var affiliateLink = listing.AffiliateLinks
            .OrderByDescending(link => link.AcquisitionMode == AffiliateLinkAcquisitionMode.OwnerProvided)
            .ThenByDescending(link => link.Status == AffiliateLinkStatus.Active)
            .ThenByDescending(link => link.CreatedAt)
            .FirstOrDefault();
        var affiliateReady = affiliateLink is not null && affiliateLink.Status == AffiliateLinkStatus.Active && affiliateLink.IsUsable(now) &&
                             (affiliateLink.HandoffMode == AffiliateHandoffMode.DirectProvider
                                 ? affiliateLink.AffiliateProgram.CanAcceptOwnerProvidedLinks()
                                 : affiliateLink.AffiliateProgram.CanGenerateLinks());
        var affiliateReadiness = affiliateLink is null ? "No retailer link is attached."
            : affiliateReady ? affiliateLink.HandoffMode == AffiliateHandoffMode.DirectProvider
                ? "Owner-provided Amazon link is ready for direct handoff."
                : "Provider-generated link is ready for protected handoff."
            : $"Retailer link is {affiliateLink.Status.ToString().ToLowerInvariant()} or its program is not ready.";
        var expired = listing.OfferValidUntil.HasValue && listing.OfferValidUntil <= now;
        var publicEligible = listing.IsEnabled && !expired && listing.Retailer.IsEnabled && listing.Product.Brand.IsEnabled && listing.Product.Category.IsEnabled && listing.MerchantPolicy.CanPublishCurrentPrice &&
                             !string.Equals(listing.MerchantPolicy.RequiredAttribution, TestOnlyAttribution, StringComparison.OrdinalIgnoreCase);
        var readiness = publicEligible ? "Ready for public discovery; retailer handoff remains derived from an approved active link."
            : expired ? $"Expired {listing.OfferValidUntil:yyyy-MM-dd HH:mm zzz}; automatically hidden from public discovery."
            : listing.IsEnabled ? "Blocked by brand, category, store, or Merchant Policy." : "Draft or deactivated; not visible in public discovery.";
        return new AdminOfferResponse(
            listing.Id, listing.ProductId, listing.Product.Slug, listing.Product.Title, listing.Product.BrandId, listing.Product.Brand.Name,
            listing.Product.CategoryId, listing.Product.Category.Name, listing.Product.ModelNumber, listing.Product.ManufacturerPartNumber, listing.Product.Gtin,
            listing.Product.VariantAttributes, listing.RetailerId, listing.Retailer.Name, listing.MerchantPolicyId, listing.MerchantPolicy.SourceKey,
            listing.ExternalListingId, listing.RetailerSku, listing.OriginalTitle, listing.ProductUrl, listing.ApprovedAffiliateDestinationReference,
            affiliateLink?.AcquisitionMode == AffiliateLinkAcquisitionMode.OwnerProvided ? affiliateLink.TrackingUrl : null,
            affiliateLink?.Provider == AffiliateProviderType.AmazonCreators ? "AMAZON_CREATORS" : affiliateLink?.Provider.ToString().ToUpperInvariant(),
            affiliateLink?.HandoffMode == AffiliateHandoffMode.DirectProvider ? "DIRECT_PROVIDER" : affiliateLink is null ? null : "INTERNAL_REDIRECT",
            affiliateLink?.Status.ToString().ToUpperInvariant(), affiliateReadiness,
            affiliateLink?.AffiliateProgram.ProviderProgramId, affiliateLink?.AffiliateProgram.RelationshipEvidenceReference,
            listing.Seller, listing.IsMarketplaceSeller, listing.Condition.ToString().ToUpperInvariant(), listing.PackQuantity, listing.BundleContents,
            listing.RegionAvailabilityContext, listing.OnlineAvailability.ToString().ToUpperInvariant(), listing.ShippingContext, listing.ExternalIdentifiers,
            listing.SourceObservedAt, listing.FetchedAt, listing.OfferValidUntil, listing.CurrentPriceAmount, listing.CurrentPriceCurrency ?? "CAD", listing.MatchState.ToString().ToUpperInvariant(),
            listing.Evidence.ToString().ToUpperInvariant(), listing.History.ToString().ToUpperInvariant(), listing.IsEnabled, publicEligible, readiness,
            $"/products/{listing.Product.Slug}");
    }

    private static AdminProductImageResponse ToProductImageResponse(ProductImage image, string productTitle, DateTimeOffset now) =>
        new(image.Id, image.ProductId, productTitle, image.FileName, image.ContentType, image.Content.Length,
            image.Width, image.Height, $"/api/v1/admin/product-images/{image.Id:D}/content", image.PublicPath, image.Origin.ToString().ToUpperInvariant(), image.State.ToString().ToUpperInvariant(),
            image.RightsEvidenceReference, image.AllowedPlacements, image.EffectiveAt, image.ExpiresAt,
            image.LastValidatedAt, image.CreatedAt, image.State == ProductImageState.Active &&
            (image.EffectiveAt is null || image.EffectiveAt <= now) && (image.ExpiresAt is null || image.ExpiresAt > now));

    private static AdminBannerResponse ToBannerResponse(Retailer retailer, StoreBannerProfile? profile, DateTimeOffset now, bool hasPublicCatalogOffer)
    {
        var visibility = profile is null ? "NOT_CONFIGURED" : !profile.IsEnabled ? "DISABLED" : profile.ExpiresAt <= now ? "EXPIRED" :
            profile.EffectiveAt > now ? "SCHEDULED" : profile.CanUseConfiguredAsset(now) ? "ENABLED" : "BLOCKED";
        var rights = profile is null ? "NOT_CONFIGURED" : profile.CanUseConfiguredAsset(now) ? "READY" : "BLOCKED";
        var isInPublicCarousel = retailer.IsEnabled && profile?.IsEnabled == true && hasPublicCatalogOffer;
        var artworkState = profile?.CanUseConfiguredAsset(now) == true ? "CONFIGURED" : "FALLBACK";
        var eligibilityReason = profile is null ? "Create and activate a banner profile." : !profile.IsEnabled ? "Banner is inactive." :
            !retailer.IsEnabled ? "Retailer is inactive." : !hasPublicCatalogOffer ? "No publicly eligible offer exists for this retailer." :
            artworkState == "FALLBACK" ? "Active with fallback GreatDeals artwork until the selected asset is ready." : "Active in the homepage carousel.";
        return new AdminBannerResponse(retailer.Id, retailer.Key, retailer.Name, profile?.Id, profile?.Title ?? $"Shop {retailer.Name}",
            profile?.Subtitle ?? "Browse current offers", profile?.AssetPath, profile?.AssetSource.ToString().ToUpperInvariant() ?? "CANADADEALSORIGINAL",
            profile?.BrandAssetPolicy.ToString().ToUpperInvariant() ?? "UNKNOWN", profile?.AssetProvider?.ToString().ToUpperInvariant(),
            profile?.AllowedPlacement, profile?.BannerOrder ?? int.MaxValue, profile?.IsEnabled ?? false, profile?.AssetEvidenceReference,
            profile?.EffectiveAt, profile?.ExpiresAt, visibility, rights, isInPublicCarousel, null, artworkState, eligibilityReason);
    }

    private AdminAuditEvent Audit(string action, string entityType, Guid entityId, string summary) =>
        AdminAuditEvent.Create(ActorId(), action, entityType, entityId, summary, clock.GetUtcNow());

    private Guid ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new InvalidOperationException("The owner admin user ID claim is missing.");

    private void ValidateHttpsUrl(string? value, string field, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required) ModelState.AddModelError(field, "An HTTPS URL is required.");
            return;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            ModelState.AddModelError(field, "Use an absolute HTTPS URL without embedded credentials.");
    }

    private void ValidateDictionary(IReadOnlyDictionary<string, string>? values, string field)
    {
        if (values is null) return;
        if (values.Count > 40 || values.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 80 || item.Value.Length > 300))
            ModelState.AddModelError(field, "Use at most 40 key/value entries; keys are limited to 80 and values to 300 characters.");
    }

    private async Task<AdminBrandCandidateResponse?> BuildBrandCandidateAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        var title = ProductTitleFromRetailerUrl(sourceUrl);
        if (title is null) return null;
        var titleKey = Brand.NormalizeKey(title);
        var brands = await db.Brands.AsNoTracking().ToListAsync(cancellationToken);
        var matched = brands
            .Where(brand => titleKey == brand.NormalizedKey || titleKey.StartsWith($"{brand.NormalizedKey} ", StringComparison.Ordinal))
            .OrderByDescending(brand => brand.NormalizedKey.Length)
            .FirstOrDefault();
        if (matched is not null)
        {
            return new AdminBrandCandidateResponse(
                matched.Name,
                matched.Slug,
                matched.NormalizedKey,
                "CATALOG_URL_PREFIX",
                "EXACT",
                matched.IsEnabled ? "MATCHED_EXISTING" : "MATCHED_INACTIVE",
                matched.Id,
                matched.Name,
                matched.IsEnabled);
        }

        var candidate = title.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        var rejected = new[] { "amazon", "best", "generic", "new", "product", "unbranded", "unknown" };
        if (candidate is null || candidate.Length is < 2 or > 80 || rejected.Contains(candidate, StringComparer.OrdinalIgnoreCase)) return null;
        var normalizedKey = Brand.NormalizeKey(candidate);
        var slug = SlugFromBrandName(candidate);
        if (normalizedKey.Length == 0 || slug.Length == 0) return null;
        return new AdminBrandCandidateResponse(candidate, slug, normalizedKey, "URL_PATH", "LOW", "NEW_CANDIDATE", null, null, null);
    }

    private static string? ProductTitleFromRetailerUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var marker = Array.FindIndex(parts, part => part.Equals("dp", StringComparison.OrdinalIgnoreCase) || part.Equals("product", StringComparison.OrdinalIgnoreCase));
        if (marker < 1) return null;
        var candidate = Uri.UnescapeDataString(parts[marker - 1]);
        candidate = Regex.Replace(candidate, "[-_]+", " ");
        candidate = Regex.Replace(candidate, "\\s+", " ").Trim();
        return candidate.Length is >= 4 and <= 240 && !Regex.IsMatch(candidate, "^[A-Z0-9]{10}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            ? candidate
            : null;
    }

    private static string SlugFromBrandName(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var ascii = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            ascii.Append(character);
        }
        return Regex.Replace(ascii.ToString().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsSupportedBannerImage(string contentType, ReadOnlySpan<byte> bytes) => contentType switch
    {
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    private sealed record OfferValidationContext(
        Product? Product,
        Brand Brand,
        bool BrandWasCreated,
        bool BrandWasActivated,
        Category Category,
        Retailer Retailer,
        MerchantPolicy Policy,
        ProductCondition Condition,
        OnlineAvailabilityState Availability,
        MatchState MatchState,
        OwnerProvidedAffiliateLinkPlan? OwnerProvidedLink);

    private sealed record BrandResolution(Brand Brand, bool WasCreated, bool WasActivated);

    private sealed record OwnerProvidedAffiliateLinkPlan(
        string TrackingUrl,
        string DestinationUrl,
        string Asin,
        string PartnerTag,
        string RelationshipEvidenceReference,
        AffiliateProgram? Program);
}
