using LendingPlatform.Api.Data;
using LendingPlatform.Api.Dtos;
using LendingPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LendingPlatform.Api.Services;

public interface ILoanApplicationService
{
    Task<LoanApplicationResponse> SubmitAsync(LoanApplicationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LoanApplicationResponse>> GetHistoryAsync(CancellationToken ct = default);
    Task<StatisticsResponse> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Coordinates the use case: score the application with the rules engine, persist the
/// result, and read it back for the API. All rule knowledge stays in LoanDecisionService.
/// </summary>
public sealed class LoanApplicationService(LendingDbContext db, ILoanDecisionService decisionService)
    : ILoanApplicationService
{
    public async Task<LoanApplicationResponse> SubmitAsync(LoanApplicationRequest request, CancellationToken ct = default)
    {
        var evaluation = decisionService.Evaluate(request.LoanAmount, request.AssetValue, request.CreditScore);

        var application = new LoanApplication
        {
            LoanAmount = request.LoanAmount,
            AssetValue = request.AssetValue,
            CreditScore = request.CreditScore,
            Ltv = evaluation.Ltv,
            Decision = evaluation.Decision,
            Reason = evaluation.Reason,
            CreatedAt = DateTime.UtcNow
        };

        db.LoanApplications.Add(application);
        await db.SaveChangesAsync(ct);

        return ToResponse(application);
    }

    public async Task<IReadOnlyList<LoanApplicationResponse>> GetHistoryAsync(CancellationToken ct = default)
    {
        var applications = await db.LoanApplications
            .AsNoTracking()
            .OrderByDescending(a => a.Id)
            .ToListAsync(ct);

        return applications.Select(ToResponse).ToList();
    }

    public async Task<StatisticsResponse> GetStatisticsAsync(CancellationToken ct = default)
    {
        // Materialised first because SQLite cannot aggregate decimals stored as TEXT.
        var applications = await db.LoanApplications.AsNoTracking().ToListAsync(ct);
        return StatisticsCalculator.Calculate(applications);
    }

    private static LoanApplicationResponse ToResponse(LoanApplication a) => new()
    {
        Id = a.Id,
        LoanAmount = a.LoanAmount,
        AssetValue = a.AssetValue,
        CreditScore = a.CreditScore,
        Ltv = a.Ltv,
        Decision = a.Decision.ToString(),
        Reason = a.Reason,
        CreatedAt = a.CreatedAt
    };
}
