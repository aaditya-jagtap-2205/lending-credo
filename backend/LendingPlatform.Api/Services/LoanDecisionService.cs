using LendingPlatform.Api.Models;

namespace LendingPlatform.Api.Services;

/// <summary>
/// The result of evaluating one application: the decision, the calculated LTV
/// and the rule that produced the outcome.
/// </summary>
/// <param name="Decision">Successful or Declined.</param>
/// <param name="Ltv">Loan to value percentage, rounded to 2dp for display/storage.</param>
/// <param name="Reason">Which rule decided the outcome.</param>
public readonly record struct LoanEvaluation(LoanDecision Decision, decimal Ltv, string Reason);

public interface ILoanDecisionService
{
    LoanEvaluation Evaluate(decimal loanAmount, decimal assetValue, int creditScore);
}

/// <summary>
/// Pure, dependency-free implementation of the lending rules. It knows nothing about
/// HTTP, EF Core or the database, so it can be unit tested in isolation.
/// </summary>
public sealed class LoanDecisionService : ILoanDecisionService
{
    public const decimal MinimumLoanAmount = 100_000m;
    public const decimal MaximumLoanAmount = 1_500_000m;
    public const decimal LargeLoanThreshold = 1_000_000m;

    public const decimal LargeLoanMaximumLtv = 60m;
    public const int LargeLoanMinimumCreditScore = 950;

    public const int MinimumCreditScore = 1;
    public const int MaximumCreditScore = 999;

    public LoanEvaluation Evaluate(decimal loanAmount, decimal assetValue, int creditScore)
    {
        // Defensive guards. The API validates before reaching here, but the engine
        // must never divide by zero or score a nonsensical application if it is
        // called from another context (tests, a future batch importer, etc).
        if (assetValue <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assetValue), "Asset value must be greater than zero; LTV cannot be calculated.");
        }

        if (loanAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(loanAmount), "Loan amount must be greater than zero.");
        }

        if (creditScore is < MinimumCreditScore or > MaximumCreditScore)
        {
            throw new ArgumentOutOfRangeException(
                nameof(creditScore), $"Credit score must be between {MinimumCreditScore} and {MaximumCreditScore}.");
        }

        // Decisions use the full-precision LTV. Only the value we display/store is rounded,
        // so an application is never approved or declined because of a rounding artefact.
        var ltv = CalculateLtv(loanAmount, assetValue);
        var displayLtv = decimal.Round(ltv, 2, MidpointRounding.AwayFromZero);

        if (loanAmount < MinimumLoanAmount)
        {
            return Declined(displayLtv, $"Loan amount is below the £{MinimumLoanAmount:N0} minimum.");
        }

        if (loanAmount > MaximumLoanAmount)
        {
            return Declined(displayLtv, $"Loan amount is above the £{MaximumLoanAmount:N0} maximum.");
        }

        if (loanAmount >= LargeLoanThreshold)
        {
            if (ltv > LargeLoanMaximumLtv)
            {
                return Declined(displayLtv, "Loans of £1,000,000 or more require an LTV of 60% or less.");
            }

            if (creditScore < LargeLoanMinimumCreditScore)
            {
                return Declined(displayLtv, "Loans of £1,000,000 or more require a credit score of at least 950.");
            }

            return Approved(displayLtv, "Meets the large loan criteria: LTV of 60% or less and a credit score of at least 950.");
        }

        // Loans under £1,000,000: the required credit score rises with the LTV band.
        if (ltv >= 90m)
        {
            return Declined(displayLtv, "An LTV of 90% or above is not accepted.");
        }

        var requiredScore = ltv switch
        {
            < 60m => 750,
            < 80m => 800,
            _ => 900   // 80% <= LTV < 90%
        };

        if (creditScore < requiredScore)
        {
            return Declined(displayLtv, $"An LTV of {displayLtv:0.##}% requires a credit score of at least {requiredScore}.");
        }

        return Approved(displayLtv, $"An LTV of {displayLtv:0.##}% requires a credit score of at least {requiredScore}, which is met.");
    }

    /// <summary>LTV as a percentage of the asset value the loan is secured against.</summary>
    public static decimal CalculateLtv(decimal loanAmount, decimal assetValue)
        => loanAmount / assetValue * 100m;

    private static LoanEvaluation Approved(decimal ltv, string reason)
        => new(LoanDecision.Successful, ltv, reason);

    private static LoanEvaluation Declined(decimal ltv, string reason)
        => new(LoanDecision.Declined, ltv, reason);
}
