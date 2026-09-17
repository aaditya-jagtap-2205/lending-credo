using LendingPlatform.Api.Dtos;
using LendingPlatform.Api.Models;

namespace LendingPlatform.Api.Services;

/// <summary>
/// Aggregates applications into the reporting figures. Kept pure (in-memory, no EF)
/// so the assumptions behind each figure can be unit tested directly.
/// </summary>
public static class StatisticsCalculator
{
    public static StatisticsResponse Calculate(IReadOnlyCollection<LoanApplication> applications)
    {
        ArgumentNullException.ThrowIfNull(applications);

        if (applications.Count == 0)
        {
            return new StatisticsResponse();
        }

        var successful = applications.Where(a => a.Decision == LoanDecision.Successful).ToList();

        return new StatisticsResponse
        {
            TotalApplications = applications.Count,
            SuccessfulApplicants = successful.Count,
            DeclinedApplicants = applications.Count - successful.Count,

            // Only successful applications result in money being lent.
            TotalValueOfLoansWritten = successful.Sum(a => a.LoanAmount),

            // "Mean average LTV across all applications" - declined applications included.
            MeanLtv = decimal.Round(
                applications.Sum(a => a.Ltv) / applications.Count, 2, MidpointRounding.AwayFromZero)
        };
    }
}
