using System.Security.Claims;
using FinTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;

    public TransactionsController(TransactionService service)
    {
        _service = service;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionReadDto>>> GetAll(
        [FromQuery] int? userId,
        [FromQuery] TimeCategory? timeCategory = null,
        [FromQuery] TimePeriod? timePeriod = null)
    {
        var authUserId = GetAuthenticatedUserId();
        if (timeCategory.HasValue && timePeriod.HasValue)
        {
            var periodTransactions = await _service.GetByTimePeriodAsync(timeCategory.Value, timePeriod.Value, authUserId);
            return Ok(periodTransactions);
        }

        var transactions = await _service.GetAllAsync(authUserId);
        return Ok(transactions);
    }

    [HttpGet("period")]
    public async Task<ActionResult<IEnumerable<TransactionReadDto>>> GetByPeriod(
        [FromQuery] TimeCategory timeCategory,
        [FromQuery] TimePeriod timePeriod,
        [FromQuery] int? userId = null)
    {
        var authUserId = GetAuthenticatedUserId();
        var transactions = await _service.GetByTimePeriodAsync(timeCategory, timePeriod, authUserId);
        return Ok(transactions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionReadDto>> GetById(int id)
    {
        var transaction = await _service.GetByIdAsync(id);
        if (transaction == null)
        {
            return NotFound(new { message = $"Transaction with ID {id} was not found." });
        }

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionReadDto>> Create([FromBody] TransactionCreateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        dto.UserID = authUserId;
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.TransactionID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TransactionUpdateDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Transaction with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] DateTime? cancellationDate = null)
    {
        var cancelled = await _service.CancelSubscriptionAsync(id, cancellationDate);
        if (!cancelled)
        {
            return NotFound(new { message = $"Transaction with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Transaction with ID {id} was not found." });
        }

        return NoContent();
    }
}