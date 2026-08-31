using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class ReceivablesController : ControllerBase
{
    private readonly ReceivableService _receivableService;

    public ReceivablesController(ReceivableService receivableService)
    {
        _receivableService = receivableService;
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
    public async Task<ActionResult<ReceivableSummaryDto>> GetSummary(int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var summary = await _receivableService.GetSummaryAsync(authUserId);
        return Ok(summary);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReceivableReadDto>> GetById(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var receivable = await _receivableService.GetByIdAsync(id, authUserId);
        if (receivable == null)
        {
            return NotFound(new { message = $"Receivable with ID {id} was not found." });
        }

        return Ok(receivable);
    }

    [HttpPost]
    public async Task<ActionResult<ReceivableReadDto>> Create([FromBody] ReceivableCreateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        dto.UserID = authUserId;
        var created = await _receivableService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.ReceivableID, userId = created.UserID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int? userId, [FromBody] ReceivableUpdateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        var updated = await _receivableService.UpdateAsync(id, authUserId, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Receivable with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("items/{itemId:int}/toggle-paid")]
    public async Task<IActionResult> ToggleItemPaid(int itemId, [FromQuery] int? userId, [FromBody] TogglePaidRequest? request)
    {
        var authUserId = GetAuthenticatedUserId();
        var success = await _receivableService.ToggleItemPaidStatusAsync(
            itemId,
            authUserId,
            request?.IsPaid,
            request?.AmountPaid,
            request?.Notes
        );

        if (!success)
        {
            return NotFound(new { message = $"Receivable item with ID {itemId} was not found." });
        }

        return Ok(new { message = "Payment status updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var deleted = await _receivableService.DeleteAsync(id, authUserId);
        if (!deleted)
        {
            return NotFound(new { message = $"Receivable with ID {id} was not found." });
        }

        return NoContent();
    }
}

public class TogglePaidRequest
{
    public bool? IsPaid { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? Notes { get; set; }
}
