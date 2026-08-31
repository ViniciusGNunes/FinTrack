using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class InvestmentsController : ControllerBase
{
    private readonly InvestmentService _investmentService;

    public InvestmentsController(InvestmentService investmentService)
    {
        _investmentService = investmentService;
    }

    private int GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Usuário não autenticado ou identificador inválido.");
    }

    [HttpGet("user/{userId:int?}")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolio(int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var summary = await _investmentService.GetPortfolioSummaryAsync(authUserId);
        return Ok(summary);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvestmentReadDto>> GetById(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var investment = await _investmentService.GetByIdAsync(id, authUserId);
        if (investment == null)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return Ok(investment);
    }

    [HttpGet("{id:int}/growth")]
    public async Task<ActionResult<List<InvestmentGrowthPointDto>>> GetGrowthHistory(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var history = await _investmentService.GetGrowthHistoryAsync(id, authUserId);
        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentReadDto>> Create([FromBody] InvestmentCreateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        dto.UserID = authUserId;
        var created = await _investmentService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.InvestmentID, userId = created.UserID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int? userId, [FromBody] InvestmentUpdateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        var updated = await _investmentService.UpdateAsync(id, authUserId, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/transactions")]
    public async Task<IActionResult> AddTransaction(int id, [FromQuery] int? userId, [FromBody] InvestmentTransactionCreateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        var success = await _investmentService.AddTransactionAsync(id, authUserId, dto);
        if (!success)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return Ok(new { message = "Transaction recorded successfully." });
    }

    [HttpPost("{id:int}/liquidate")]
    public async Task<IActionResult> Liquidate(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var liquidated = await _investmentService.LiquidateAsync(id, authUserId);
        if (!liquidated)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var deleted = await _investmentService.DeleteAsync(id, authUserId);
        if (!deleted)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return NoContent();
    }
}
