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

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolio(int userId)
    {
        var summary = await _investmentService.GetPortfolioSummaryAsync(userId);
        return Ok(summary);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvestmentReadDto>> GetById(int id, [FromQuery] int userId)
    {
        var investment = await _investmentService.GetByIdAsync(id, userId);
        if (investment == null)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return Ok(investment);
    }

    [HttpGet("{id:int}/growth")]
    public async Task<ActionResult<List<InvestmentGrowthPointDto>>> GetGrowthHistory(int id, [FromQuery] int userId)
    {
        var history = await _investmentService.GetGrowthHistoryAsync(id, userId);
        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentReadDto>> Create([FromBody] InvestmentCreateDto dto)
    {
        var created = await _investmentService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.InvestmentID, userId = created.UserID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] InvestmentUpdateDto dto)
    {
        var updated = await _investmentService.UpdateAsync(id, userId, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/transactions")]
    public async Task<IActionResult> AddTransaction(int id, [FromQuery] int userId, [FromBody] InvestmentTransactionCreateDto dto)
    {
        var success = await _investmentService.AddTransactionAsync(id, userId, dto);
        if (!success)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return Ok(new { message = "Transaction recorded successfully." });
    }

    [HttpPost("{id:int}/liquidate")]
    public async Task<IActionResult> Liquidate(int id, [FromQuery] int userId)
    {
        var liquidated = await _investmentService.LiquidateAsync(id, userId);
        if (!liquidated)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
    {
        var deleted = await _investmentService.DeleteAsync(id, userId);
        if (!deleted)
        {
            return NotFound(new { message = $"Investment with ID {id} was not found." });
        }

        return NoContent();
    }
}
