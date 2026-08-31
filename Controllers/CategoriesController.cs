using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

[ApiController]
[Route("v1/api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _service;

    public CategoriesController(CategoryService service)
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

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryReadDto>>> GetForUser([FromQuery] int? userId)
    {
        var authUserId = GetAuthenticatedUserId();
        var categories = await _service.GetForUserAsync(authUserId);
        return Ok(categories);
    }

    // GET: api/categories/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryReadDto>> GetById(int id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound(new { message = $"Category with ID {id} was not found." });
        }

        return Ok(category);
    }

    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<CategoryReadDto>> Create([FromBody] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var authUserId = GetAuthenticatedUserId();
        dto.UserID = authUserId;
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.CategoryID }, created);
    }

    // PUT: api/categories/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateDto dto, [FromQuery] int? userId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var authUserId = GetAuthenticatedUserId();
            var updated = await _service.UpdateAsync(id, dto, authUserId);
            if (!updated)
            {
                return NotFound(new { message = $"Category with ID {id} was not found." });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    // DELETE: api/categories/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
    {
        try
        {
            var authUserId = GetAuthenticatedUserId();
            var deleted = await _service.DeleteAsync(id, authUserId);
            if (!deleted)
            {
                return NotFound(new { message = $"Category with ID {id} was not found." });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST: api/categories/seed
    [HttpPost("seed")]
    public async Task<IActionResult> SeedDefaults()
    {
        await _service.SeedDefaultCategoriesAsync();
        return Ok(new { message = "Default categories seeded successfully." });
    }
}