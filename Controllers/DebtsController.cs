using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class DebtsController : ControllerBase
{
    private readonly DebtService _debtService;

    public DebtsController(DebtService debtService)
    {
        _debtService = debtService;
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
    public async Task<ActionResult<DebtSummaryDto>> GetSummary(int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var summary = await _debtService.GetSummaryAsync(authUserId);
        return Ok(summary);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DebtReadDto>> GetById(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var debt = await _debtService.GetByIdAsync(id, authUserId);
        if (debt == null)
        {
            return NotFound(new { message = $"Debt with ID {id} was not found." });
        }

        return Ok(debt);
    }

    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult<List<DebtScheduleItemDto>>> GetSchedule(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var schedule = await _debtService.GetScheduleAsync(id, authUserId);
        return Ok(schedule);
    }

    [HttpPost]
    public async Task<ActionResult<DebtReadDto>> Create([FromBody] DebtCreateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        dto.UserID = authUserId;
        var created = await _debtService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.DebtID, userId = created.UserID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int? userId, [FromBody] DebtUpdateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        var updated = await _debtService.UpdateAsync(id, authUserId, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Debt with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> RecordPayment(int id, [FromQuery] int? userId, [FromBody] DebtPaymentCreateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        var success = await _debtService.RecordPaymentAsync(id, authUserId, dto);
        if (!success)
        {
            return NotFound(new { message = $"Debt with ID {id} was not found." });
        }

        return Ok(new { message = "Payment recorded successfully." });
    }

    [HttpPost("{id:int}/payoff")]
    public async Task<IActionResult> Payoff(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var success = await _debtService.PayoffDebtAsync(id, authUserId);
        if (!success)
        {
            return NotFound(new { message = $"Debt with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var deleted = await _debtService.DeleteAsync(id, authUserId);
        if (!deleted)
        {
            return NotFound(new { message = $"Debt with ID {id} was not found." });
        }

        return NoContent();
    }
}
