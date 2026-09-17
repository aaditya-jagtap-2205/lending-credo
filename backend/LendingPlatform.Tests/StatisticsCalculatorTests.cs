using LendingPlatform.Api.Models;
using LendingPlatform.Api.Services;

namespace LendingPlatform.Tests;

public class StatisticsCalculatorTests
{
    private static LoanApplication App(decimal loanAmount, decimal ltv, LoanDecision decision) => new()
    {
        LoanAmount = loanAmount,
        AssetValue = loanAmount / ltv * 100m,
        CreditScore = 800,
        Ltv = ltv,
        Decision = decision,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public void NoApplications_ReturnsZeroes()
    {
        var stats = StatisticsCalculator.Calculate([]);

        Assert.Equal(0, stats.TotalApplications);
        Assert.Equal(0, stats.SuccessfulApplicants);
        Assert.Equal(0, stats.DeclinedApplicants);
        Assert.Equal(0m, stats.TotalValueOfLoansWritten);
        Assert.Equal(0m, stats.MeanLtv);
    }

    [Fact]
    public void CountsApplicantsByDecision()
    {
        var stats = StatisticsCalculator.Calculate(
        [
            App(200_000m, 50m, LoanDecision.Successful),
            App(300_000m, 60m, LoanDecision.Successful),
            App(400_000m, 95m, LoanDecision.Declined)
        ]);

        Assert.Equal(3, stats.TotalApplications);
        Assert.Equal(2, stats.SuccessfulApplicants);
        Assert.Equal(1, stats.DeclinedApplicants);
    }

    [Fact]
    public void TotalValueOfLoansWritten_CountsSuccessfulApplicationsOnly()
    {
        var stats = StatisticsCalculator.Calculate(
        [
            App(200_000m, 50m, LoanDecision.Successful),
            App(300_000m, 60m, LoanDecision.Successful),
            App(999_999m, 95m, LoanDecision.Declined)
        ]);

        Assert.Equal(500_000m, stats.TotalValueOfLoansWritten);
    }

    [Fact]
    public void MeanLtv_IncludesDeclinedApplications()
    {
        // (50 + 60 + 100) / 3 = 70
        var stats = StatisticsCalculator.Calculate(
        [
            App(200_000m, 50m, LoanDecision.Successful),
            App(300_000m, 60m, LoanDecision.Successful),
            App(400_000m, 100m, LoanDecision.Declined)
        ]);

        Assert.Equal(70m, stats.MeanLtv);
    }

    [Fact]
    public void MeanLtv_IsRoundedToTwoDecimalPlaces()
    {
        // (50 + 60 + 65) / 3 = 58.333...
        var stats = StatisticsCalculator.Calculate(
        [
            App(200_000m, 50m, LoanDecision.Successful),
            App(300_000m, 60m, LoanDecision.Successful),
            App(400_000m, 65m, LoanDecision.Declined)
        ]);

        Assert.Equal(58.33m, stats.MeanLtv);
    }

    [Fact]
    public void AllApplicationsDeclined_LeavesNothingWrittenButStillReportsMeanLtv()
    {
        var stats = StatisticsCalculator.Calculate(
        [
            App(400_000m, 95m, LoanDecision.Declined),
            App(500_000m, 91m, LoanDecision.Declined)
        ]);

        Assert.Equal(0m, stats.TotalValueOfLoansWritten);
        Assert.Equal(93m, stats.MeanLtv);
    }
}
