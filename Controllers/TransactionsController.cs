using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;

    public TransactionsController(TransactionService service)
    {
        _service = service;
    }

    // GET: api/transactions?userId=1
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionReadDto>>> GetAll([FromQuery] int? userId)
    {
        var transactions = await _service.GetAllAsync(userId);
        return Ok(transactions);
    }

    // GET: api/transactions/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionReadDto>> GetById(int id)
    {
        var transaction = await _service.GetByIdAsync(id);
        if (transaction == null)
        {
            return NotFound($"Transaction with ID {id} was not found.");
        }

        return Ok(transaction);
    }

    // POST: api/transactions
    [HttpPost]
    public async Task<ActionResult<TransactionReadDto>> Create([FromBody] TransactionCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.TransactionID }, created);
    }

    // PUT: api/transactions/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TransactionUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _service.UpdateAsync(id, dto);
        if (!updated)
        {
            return NotFound($"Transaction with ID {id} was not found.");
        }

        return NoContent();
    }

    // DELETE: api/transactions/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound($"Transaction with ID {id} was not found.");
        }

        return NoContent();
    }
}