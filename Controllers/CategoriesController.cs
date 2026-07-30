using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _service;

    public CategoriesController(CategoryService service)
    {
        _service = service;
    }

    // GET: api/categories?userId=25
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryReadDto>>> GetForUser([FromQuery] int userId)
    {
        var categories = await _service.GetForUserAsync(userId);
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

        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.CategoryID }, created);
    }

    // PUT: api/categories/5?userId=25
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateDto dto, [FromQuery] int? userId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _service.UpdateAsync(id, dto, userId);
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

    // DELETE: api/categories/5?userId=25
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, userId);
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