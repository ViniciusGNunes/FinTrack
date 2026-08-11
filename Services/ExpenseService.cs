using FinTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;
public class ExpenseService
{
    private readonly AppDbContext _context;

    public ExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DetailedExpenseReadDto>> GetMonthlyExpensesAsync(int userId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Transaction)
                .ThenInclude(t => t!.Category)
            .Where(e => e.UserID == userId
                     && e.DueDate >= startDate
                     && e.DueDate <= endDate
                     && e.Status != ExpenseStatus.Cancelled)
            .OrderBy(e => e.DueDate)
            .ToListAsync();

        return expenses.Select(MapToDetailedDto);
    }

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
                     && e.Status != ExpenseStatus.Refunded
                     && e.DueDate < now)
            .OrderBy(e => e.DueDate)
            .ToListAsync();

        return expenses.Select(MapToDetailedDto);
    }

    public async Task<DetailedExpenseReadDto?> GetByIdAsync(int id)
    {
        var expense = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Transaction)
                .ThenInclude(t => t!.Category)
            .FirstOrDefaultAsync(e => e.ExpenseID == id);

        return expense == null ? null : MapToDetailedDto(expense);
    }

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

    public async Task<bool> ProcessPartialPaymentAsync(int id, PartialPayExpenseDto dto)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return false;

        expense.PaidAmount += dto.PaymentAmount;
        expense.PaidDate = dto.PaidDate ?? DateTime.UtcNow;
        expense.UpdatedAtUtc = DateTime.UtcNow;

        if (expense.PaidAmount >= expense.Amount)
        {
            expense.PaidAmount = expense.Amount;
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

    public async Task<bool> RegisterRefundAsync(int id, RefundExpenseDto dto)
    {
        var expense = await _context.Expenses
            .Include(e => e.Transaction)
            .FirstOrDefaultAsync(e => e.ExpenseID == id);

        if (expense == null) return false;

        if (expense.RefundedAmount + dto.RefundAmount > expense.Amount)
        {
            throw new InvalidOperationException("Refund amount cannot exceed the total expense amount.");
        }

        expense.RefundedAmount += dto.RefundAmount;
        expense.RefundDate = dto.RefundDate ?? DateTime.UtcNow;
        expense.RefundReason = dto.Reason;
        expense.UpdatedAtUtc = DateTime.UtcNow;

        expense.Status = expense.RefundedAmount >= expense.Amount
            ? ExpenseStatus.Refunded
            : ExpenseStatus.PartiallyRefunded;

        // Propaga o valor reembolsado para a Transaction pai
        if (expense.Transaction != null)
        {
            expense.Transaction.RefundedAmount += dto.RefundAmount;
            expense.Transaction.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAmountAsync(int id, UpdateExpenseAmountDto dto)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return false;

        expense.Amount = dto.NewAmount;
        expense.UpdatedAtUtc = DateTime.UtcNow;

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
            RefundedAmount = e.RefundedAmount,
            DueDate = e.DueDate,
            PaidDate = e.PaidDate,
            RefundDate = e.RefundDate,
            RefundReason = e.RefundReason,
            CurrentInstallment = e.CurrentInstallment,
            TotalInstallments = e.Transaction?.TotalInstallments ?? 1,
            IsInstallment = e.Transaction?.IsInstallment ?? false,
            Status = e.Status,
            UserID = e.UserID
        };
    }
}
