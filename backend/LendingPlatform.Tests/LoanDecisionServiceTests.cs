using LendingPlatform.Api.Models;
using LendingPlatform.Api.Services;

namespace LendingPlatform.Tests;

/// <summary>
/// Tests are grouped by the rule they exercise and concentrate on the boundaries,
/// because that is where this rule set is easiest to get wrong.
/// </summary>
public class LoanDecisionServiceTests
{
    private readonly LoanDecisionService _sut = new();

    // ---------- LTV calculation ----------

    [Theory]
    [InlineData(500_000, 1_000_000, 50)]
    [InlineData(900_000, 1_000_000, 90)]
    [InlineData(100_000, 100_000, 100)]
    [InlineData(333_333, 1_000_000, 33.3333)]
    public void CalculateLtv_ReturnsLoanAsPercentageOfAssetValue(decimal loan, decimal asset, decimal expected)
    {
        Assert.Equal(expected, LoanDecisionService.CalculateLtv(loan, asset));
    }

    // ---------- General limits ----------

    [Fact]
    public void JustUnderMinimumLoanAmount_IsDeclined()
    {
        var result = _sut.Evaluate(99_999.99m, 1_000_000m, 999);
        Assert.Equal(LoanDecision.Declined, result.Decision);
    }

    [Fact]
    public void ExactlyMinimumLoanAmount_IsAccepted()
    {
        // £100,000 is inclusive: "decline if < £100,000".
        var result = _sut.Evaluate(100_000m, 1_000_000m, 750);
        Assert.Equal(LoanDecision.Successful, result.Decision);
    }

    [Fact]
    public void ExactlyMaximumLoanAmount_IsAccepted()
    {
        // £1,500,000 is inclusive, and is a large loan: LTV <= 60% and score >= 950.
        var result = _sut.Evaluate(1_500_000m, 2_500_000m, 950);
        Assert.Equal(LoanDecision.Successful, result.Decision);
    }

    [Fact]
    public void JustOverMaximumLoanAmount_IsDeclined()
    {
        var result = _sut.Evaluate(1_500_000.01m, 10_000_000m, 999);
        Assert.Equal(LoanDecision.Declined, result.Decision);
    }

    // ---------- The £1,000,000 threshold ----------

    [Fact]
    public void ExactlyOneMillion_UsesLargeLoanRules()
    {
        // LTV 80% with a perfect score: acceptable under the small-loan ladder,
        // but a £1m loan must be at 60% LTV or less, so it is declined.
        var result = _sut.Evaluate(1_000_000m, 1_250_000m, 999);
        Assert.Equal(LoanDecision.Declined, result.Decision);
    }

    [Fact]
    public void JustUnderOneMillion_UsesSmallLoanRules()
    {
        // One penny less, same security: now scored on the "< 80%" band, so 800 is enough.
        var result = _sut.Evaluate(999_999.99m, 1_250_000m, 800);
        Assert.Equal(LoanDecision.Successful, result.Decision);
    }

    [Theory]
    // Asset value is fixed at £2,000,000, so the loan amount sets the LTV exactly.
    [InlineData(1_200_000, 950, true)]    // LTV 60% exactly - allowed, the rule is "60% or less"
    [InlineData(1_199_800, 950, true)]    // LTV 59.99%
    [InlineData(1_200_200, 999, false)]   // LTV 60.01% - just over the limit
    [InlineData(1_200_000, 949, false)]   // credit score one below 950
    [InlineData(1_200_000, 951, true)]
    public void LargeLoan_RequiresLtvAtMostSixtyAndScoreAtLeast950(decimal loan, int creditScore, bool expectedSuccess)
    {
        var result = _sut.Evaluate(loan, 2_000_000m, creditScore);

        Assert.Equal(expectedSuccess ? LoanDecision.Successful : LoanDecision.Declined, result.Decision);
    }

    // ---------- Small loan LTV / credit score ladder ----------

    [Theory]
    // Asset value is fixed at £1,000,000, so the loan amount sets the LTV exactly.
    // LTV < 60% -> score >= 750
    [InlineData(599_900, 750, true)]    // LTV 59.99%
    [InlineData(599_900, 749, false)]
    // LTV of exactly 60% is not "< 60%", so it falls into the "< 80%" band -> score >= 800
    [InlineData(600_000, 799, false)]
    [InlineData(600_000, 800, true)]
    [InlineData(799_900, 800, true)]    // LTV 79.99%
    [InlineData(799_900, 799, false)]
    // LTV of exactly 80% falls into the "< 90%" band -> score >= 900
    [InlineData(800_000, 899, false)]
    [InlineData(800_000, 900, true)]
    [InlineData(899_900, 900, true)]    // LTV 89.99%
    [InlineData(899_900, 899, false)]
    // LTV >= 90% is declined whatever the credit score
    [InlineData(900_000, 999, false)]
    [InlineData(950_000, 999, false)]
    public void SmallLoan_RequiredScoreRisesWithLtvBand(decimal loan, int creditScore, bool expectedSuccess)
    {
        var result = _sut.Evaluate(loan, 1_000_000m, creditScore);

        Assert.Equal(expectedSuccess ? LoanDecision.Successful : LoanDecision.Declined, result.Decision);
    }

    [Fact]
    public void Decision_UsesUnroundedLtv()
    {
        // True LTV is 89.999...%, which is under 90 and so is scored in the 80-90 band.
        // If the engine rounded to 90.00 before deciding, this would wrongly decline.
        var result = _sut.Evaluate(899_999m, 1_000_000m, 900);

        Assert.Equal(LoanDecision.Successful, result.Decision);
        Assert.Equal(90m, result.Ltv); // the *displayed* value is still rounded to 90.00
    }

    // ---------- Invalid input ----------

    [Fact]
    public void AssetValueOfZero_Throws_BecauseLtvCannotBeCalculated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Evaluate(200_000m, 0m, 800));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-500_000)]
    public void NegativeAssetValue_Throws(decimal assetValue)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Evaluate(200_000m, assetValue, 800));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-250_000)]
    public void NonPositiveLoanAmount_Throws(decimal loanAmount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Evaluate(loanAmount, 1_000_000m, 800));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1000)]
    public void CreditScoreOutsideOneToNineNineNine_Throws(int creditScore)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Evaluate(200_000m, 1_000_000m, creditScore));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    public void CreditScoreAtTheEdgesOfTheValidRange_IsAccepted(int creditScore)
    {
        var exception = Record.Exception(() => _sut.Evaluate(200_000m, 1_000_000m, creditScore));
        Assert.Null(exception);
    }

    // ---------- Representative end-to-end cases ----------

    [Fact]
    public void TypicalSuccessfulApplication()
    {
        var result = _sut.Evaluate(400_000m, 800_000m, 850); // LTV 50%, score 850

        Assert.Equal(LoanDecision.Successful, result.Decision);
        Assert.Equal(50m, result.Ltv);
        Assert.NotEmpty(result.Reason);
    }

    [Fact]
    public void TypicalDeclinedApplication_ExplainsWhy()
    {
        var result = _sut.Evaluate(750_000m, 1_000_000m, 760); // LTV 75%, needs 800

        Assert.Equal(LoanDecision.Declined, result.Decision);
        Assert.Equal(75m, result.Ltv);
        Assert.Contains("800", result.Reason);
    }
}
