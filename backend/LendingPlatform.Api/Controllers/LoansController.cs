using LendingPlatform.Api.Dtos;
using LendingPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LendingPlatform.Api.Controllers;

/// <summary>
/// HTTP entry point only: model binding, status codes and delegation.
/// No lending rules live here.
/// </summary>
[ApiController]
[Route("api/loans")]
[Produces("application/json")]
public class LoansController(ILoanApplicationService loanApplicationService) : ControllerBase
{
    /// <summary>Submit a loan application and receive a decision.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LoanApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoanApplicationResponse>> Submit(
        [FromBody] LoanApplicationRequest request, CancellationToken ct)
    {
        var result = await loanApplicationService.SubmitAsync(request, ct);
        return CreatedAtAction(nameof(GetHistory), new { id = result.Id }, result);
    }

    /// <summary>Full application history, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LoanApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LoanApplicationResponse>>> GetHistory(CancellationToken ct)
        => Ok(await loanApplicationService.GetHistoryAsync(ct));

    /// <summary>Applicant counts, total value lent and mean LTV.</summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(StatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatisticsResponse>> GetStatistics(CancellationToken ct)
        => Ok(await loanApplicationService.GetStatisticsAsync(ct));
}
