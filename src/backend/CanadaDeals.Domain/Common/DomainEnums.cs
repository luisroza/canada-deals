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
