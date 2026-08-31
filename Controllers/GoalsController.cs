using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class GoalsController : ControllerBase
{
    private readonly GoalService _goalService;

    public GoalsController(GoalService goalService)
    {
        _goalService = goalService;
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
    public async Task<ActionResult<GoalSummaryDto>> GetSummary(int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var summary = await _goalService.GetSummaryAsync(authUserId);
        return Ok(summary);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoalReadDto>> GetById(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var goal = await _goalService.GetByIdAsync(id, authUserId);
        if (goal == null)
        {
            return NotFound(new { message = $"Goal with ID {id} was not found." });
        }

        return Ok(goal);
    }

    [HttpPost]
    public async Task<ActionResult<GoalReadDto>> Create([FromBody] GoalCreateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        dto.UserID = authUserId;
        var created = await _goalService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.GoalID, userId = created.UserID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int? userId, [FromBody] GoalUpdateDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        var updated = await _goalService.UpdateAsync(id, authUserId, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Goal with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/progress")]
    public async Task<IActionResult> LogProgress(int id, [FromQuery] int? userId, [FromBody] GoalLogProgressDto dto)
    {
        var authUserId = GetAuthenticatedUserId();
        var success = await _goalService.LogProgressAsync(id, authUserId, dto);
        if (!success)
        {
            return NotFound(new { message = $"Goal with ID {id} was not found." });
        }

        return Ok(new { message = "Goal progress updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var deleted = await _goalService.DeleteAsync(id, authUserId);
        if (!deleted)
        {
            return NotFound(new { message = $"Goal with ID {id} was not found." });
        }

        return NoContent();
    }
}
