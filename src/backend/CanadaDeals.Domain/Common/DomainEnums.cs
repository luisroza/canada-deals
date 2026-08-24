namespace CanadaDeals.Domain.Common;

public enum PolicyPermission
{
    Unknown = 0,
    Allowed = 1,
    Denied = 2
}

public enum FreshnessState
{
    Unknown = 0,
    Recent = 1,
    Aging = 2,
    Stale = 3
}

public enum EvidenceState
{
    Unknown = 0,
    Strong = 1,
    Partial = 2,
    Unavailable = 3
}

public enum HistoryAvailability
{
    Reliable = 0,
    Partial = 1,
    Unavailable = 2
}

public enum MatchState
{
    AutoMatched = 0,
    Confirmed = 1,
    PossibleMatchReview = 2,
    NoMatch = 3,
    ManualReview = 4
}

public enum OnlineAvailabilityState
{
    Unknown = 0,
    Available = 1,
    Unavailable = 2
}

public enum ProductCondition
{
    Unknown = 0,
    New = 1,
    Used = 2,
    Refurbished = 3
}

public enum AffiliateProviderType
{
    Unknown = 0,
    Impact = 1,
    Cj = 2,
    AmazonCreators = 3,
    Other = 4,
    Rakuten = 5
}

public enum AffiliateProgramStatus
{
    PendingApproval = 0,
    Active = 1,
    Suspended = 2,
    Expired = 3,
    Disabled = 4,
    ConfigurationIncomplete = 5
}

public enum AffiliateLinkStatus
{
    Pending = 0,
    Active = 1,
    Invalid = 2,
    Disabled = 3
}

public enum StoreBannerAssetSource
{
    CanadaDealsOriginal = 0,
    MerchantApprovedAffiliateAsset = 1
}

public enum IntegrationAdvertiserStatus
{
    Unknown = 0,
    Active = 1,
    Inactive = 2
}

public enum IntegrationPartnershipStatus
{
    Unknown = 0,
    Active = 1,
    Pending = 2,
    SelfRemoved = 3,
    PermanentDecline = 4,
    PermanentRemove = 5,
    TemporaryDecline = 6,
    TemporaryRemove = 7,
    Extended = 8
}

public enum IntegrationRunStatus
{
    Running = 0,
    Succeeded = 1,
    Failed = 2,
    Blocked = 3
}
