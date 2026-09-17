using System.ComponentModel.DataAnnotations;

namespace LendingPlatform.Api.Dtos;

/// <summary>Incoming loan application submitted by an applicant.</summary>
public class LoanApplicationRequest
{
    /// <example>750000</example>
    [Required]
    [Range(0.01, 100_000_000, ErrorMessage = "Loan amount must be a positive value.")]
    public decimal LoanAmount { get; set; }

    /// <example>1000000</example>
    [Required]
    [Range(0.01, 1_000_000_000, ErrorMessage = "Asset value must be greater than zero.")]
    public decimal AssetValue { get; set; }

    /// <example>820</example>
    [Required]
    [Range(1, 999, ErrorMessage = "Credit score must be between 1 and 999.")]
    public int CreditScore { get; set; }
}

/// <summary>A stored application and its decision, as returned by the API.</summary>
public class LoanApplicationResponse
{
    public int Id { get; set; }
    public decimal LoanAmount { get; set; }
    public decimal AssetValue { get; set; }
    public int CreditScore { get; set; }
    public decimal Ltv { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Aggregate figures across the applications written to date.</summary>
public class StatisticsResponse
{
    public int TotalApplications { get; set; }
    public int SuccessfulApplicants { get; set; }
    public int DeclinedApplicants { get; set; }

    /// <summary>Sum of loan amounts for successful applications only.</summary>
    public decimal TotalValueOfLoansWritten { get; set; }

    /// <summary>Mean LTV across all applications, successful or not.</summary>
    public decimal MeanLtv { get; set; }
}
