namespace LendingPlatform.Api.Models;

/// <summary>
/// Persisted record of a submitted loan application and the decision it received.
/// </summary>
public class LoanApplication
{
    public int Id { get; set; }

    public decimal LoanAmount { get; set; }

    public decimal AssetValue { get; set; }

    public int CreditScore { get; set; }

    /// <summary>Loan to value as a percentage, rounded to 2 decimal places for display.</summary>
    public decimal Ltv { get; set; }

    public LoanDecision Decision { get; set; }

    /// <summary>Human readable explanation of why the decision was reached.</summary>
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
