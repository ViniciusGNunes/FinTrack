using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
public class ExpenseService
{
    private readonly AppDbContext _context;

    public ExpenseService(AppDbContext context)
    {
        _context = context;
    }

    // 1. DASHBOARD QUERY: Get Expenses by Month/Year
    public async Task<IEnumerable<DetailedExpenseReadDto>> GetMonthlyExpensesAsync(int userId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Transaction)
                .ThenInclude(t => t!.Category)
            .Where(e => e.UserID == userId && e.DueDate >= startDate && e.DueDate <= endDate)
            .OrderBy(e => e.DueDate)
            .ToListAsync();

        return expenses.Select(MapToDetailedDto);
    }

    // 2. DASHBOARD QUERY: Get Overdue Expenses
    public async Task<IEnumerable<DetailedExpenseReadDto>> GetOverdueExpensesAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Transaction)
                .ThenInclude(t => t!.Category)
            .Where(e => e.UserID == userId 
                     && e.Status != ExpenseStatus.Paid 
                     && e.Status != ExpenseStatus.Cancelled
                     && e.DueDate < now)
            .OrderBy(e => e.DueDate)
            .ToListAsync();

        return expenses.Select(MapToDetailedDto);
    }

    // 3. GET BY ID
    public async Task<DetailedExpenseReadDto?> GetByIdAsync(int id)
    {
        var expense = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Transaction)
                .ThenInclude(t => t!.Category)
            .FirstOrDefaultAsync(e => e.ExpenseID == id);

        return expense == null ? null : MapToDetailedDto(expense);
    }

    // 4. MARK FULLY PAID
    public async Task<bool> MarkAsPaidAsync(int id, PayExpenseDto dto)
    {
        var expense = await _context.Expenses
            .Include(e => e.Transaction)
            .FirstOrDefaultAsync(e => e.ExpenseID == id);

        if (expense == null) return false;

        expense.PaidAmount = expense.Amount;
        expense.PaidDate = dto.PaidDate ?? DateTime.UtcNow;
        expense.Status = ExpenseStatus.Paid;
        expense.UpdatedAtUtc = DateTime.UtcNow;

        await CheckAndCompleteParentTransactionAsync(expense.TransactionID);
        await _context.SaveChangesAsync();
        return true;
    }

    // 5. PARTIAL PAYMENT
    public async Task<bool> ProcessPartialPaymentAsync(int id, PartialPayExpenseDto dto)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return false;

        expense.PaidAmount += dto.PaymentAmount;
        expense.PaidDate = dto.PaidDate ?? DateTime.UtcNow;
        expense.UpdatedAtUtc = DateTime.UtcNow;

        if (expense.PaidAmount >= expense.Amount)
        {
            expense.PaidAmount = expense.Amount; // Clamp
            expense.Status = ExpenseStatus.Paid;
            await CheckAndCompleteParentTransactionAsync(expense.TransactionID);
        }
        else
        {
            expense.Status = ExpenseStatus.PartiallyPaid;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // 6. UPDATE AMOUNT (for Variable Monthly Bills like AWS)
    public async Task<bool> UpdateAmountAsync(int id, UpdateExpenseAmountDto dto)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return false;

        expense.Amount = dto.NewAmount;
        expense.UpdatedAtUtc = DateTime.UtcNow;

        // Recalculate status in case paid amount now covers it or drops back down
        if (expense.PaidAmount >= expense.Amount && expense.Amount > 0)
        {
            expense.Status = ExpenseStatus.Paid;
        }
        else if (expense.PaidAmount > 0)
        {
            expense.Status = ExpenseStatus.PartiallyPaid;
        }
        else
        {
            expense.Status = ExpenseStatus.Pending;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // Helper: Check if all child expenses are paid; if so, mark parent Transaction as Completed
    private async Task CheckAndCompleteParentTransactionAsync(int transactionId)
    {
        var parent = await _context.Transactions
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.TransactionID == transactionId);

        if (parent != null && !parent.IsRecurrent && parent.Expenses.All(e => e.Status == ExpenseStatus.Paid))
        {
            parent.Status = TransactionStatus.Completed;
            parent.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    // Mapping Helper
    private static DetailedExpenseReadDto MapToDetailedDto(Expense e)
    {
        return new DetailedExpenseReadDto
        {
            ExpenseID = e.ExpenseID,
            TransactionID = e.TransactionID,
            TransactionName = e.Transaction?.Name ?? "Unknown Transaction",
            CategoryName = e.Transaction?.Category?.Name ?? "Uncategorized",
            PaymentMethod = e.Transaction?.PaymentMethod ?? PaymentMethod.Cash,
            Amount = e.Amount,
            PaidAmount = e.PaidAmount,
            DueDate = e.DueDate,
            PaidDate = e.PaidDate,
            CurrentInstallment = e.CurrentInstallment,
            TotalInstallments = e.Transaction?.TotalInstallments ?? 1,
            IsInstallment = e.Transaction?.IsInstallment ?? false,
            Status = e.Status,
            UserID = e.UserID
        };
    }
}

