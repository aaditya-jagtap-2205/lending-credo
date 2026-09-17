namespace LendingPlatform.Api.Models;

/// <summary>
/// The outcome of evaluating a loan application against the lending rules.
/// </summary>
public enum LoanDecision
{
    Declined = 0,
    Successful = 1
}
