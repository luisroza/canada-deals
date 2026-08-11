using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Matching;

namespace CanadaDeals.Domain.Tests;

public sealed class MatchingRulesTests
{
    [Fact]
    public void Trusted_identifier_auto_matches()
    {
        Assert.Equal(MatchState.AutoMatched, MatchingRules.Determine("123", null, null, null, false));
    }

    [Fact]
    public void Brand_and_model_can_confirm_match()
    {
        Assert.Equal(MatchState.Confirmed, MatchingRules.Determine(null, "Brand", "Model-1", null, false));
    }

    [Fact]
    public void Title_only_is_review_not_safe_confirmation()
    {
        Assert.Equal(MatchState.PossibleMatchReview, MatchingRules.Determine(null, null, null, null, true));
    }
}
