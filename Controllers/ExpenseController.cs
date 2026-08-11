using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _service;

    public ExpensesController(ExpenseService service)
    {
        _service = service;
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<IEnumerable<DetailedExpenseReadDto>>> GetMonthly(
        [FromQuery] int userId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var expenses = await _service.GetMonthlyExpensesAsync(userId, month, year);
        return Ok(expenses);
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<DetailedExpenseReadDto>>> GetOverdue([FromQuery] int userId)
    {
        var expenses = await _service.GetOverdueExpensesAsync(userId);
        return Ok(expenses);
    }

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

    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> RegisterRefund(int id, [FromBody] RefundExpenseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _service.RegisterRefundAsync(id, dto);
            if (!updated)
            {
                return NotFound(new { message = $"Expense with ID {id} was not found." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

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