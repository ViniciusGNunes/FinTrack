using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _service;

    public ExpensesController(ExpenseService service)
    {
        _service = service;
    }

    // GET: api/expenses/monthly?userId=1&month=8&year=2026
    [HttpGet("monthly")]
    public async Task<ActionResult<IEnumerable<DetailedExpenseReadDto>>> GetMonthly(
        [FromQuery] int userId, 
        [FromQuery] int month, 
        [FromQuery] int year)
    {
        var expenses = await _service.GetMonthlyExpensesAsync(userId, month, year);
        return Ok(expenses);
    }

    // GET: api/expenses/overdue?userId=1
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<DetailedExpenseReadDto>>> GetOverdue([FromQuery] int userId)
    {
        var expenses = await _service.GetOverdueExpensesAsync(userId);
        return Ok(expenses);
    }

    // GET: api/expenses/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DetailedExpenseReadDto>> GetById(int id)
    {
        var expense = await _service.GetByIdAsync(id);
        if (expense == null)
        {
            return NotFound(new { message = $"Expense with ID {id} was not found." });
        }

        return Ok(expense);
    }

    // POST: api/expenses/5/pay
    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> MarkAsPaid(int id, [FromBody] PayExpenseDto dto)
    {
        var updated = await _service.MarkAsPaidAsync(id, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Expense with ID {id} was not found." });
        }

        return NoContent();
    }

    // POST: api/expenses/5/partial-pay
    [HttpPost("{id:int}/partial-pay")]
    public async Task<IActionResult> PartialPay(int id, [FromBody] PartialPayExpenseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _service.ProcessPartialPaymentAsync(id, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Expense with ID {id} was not found." });
        }

        return NoContent();
    }

    // PATCH: api/expenses/5/amount
    [HttpPatch("{id:int}/amount")]
    public async Task<IActionResult> UpdateAmount(int id, [FromBody] UpdateExpenseAmountDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _service.UpdateAmountAsync(id, dto);
        if (!updated)
        {
            return NotFound(new { message = $"Expense with ID {id} was not found." });
        }

        return NoContent();
    }
}