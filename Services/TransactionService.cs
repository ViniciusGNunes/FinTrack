using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class TransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    // BROWSE / GET ALL
    public async Task<IEnumerable<TransactionReadDto>> GetAllAsync(int? userId = null)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Expenses)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserID == userId.Value);
        }

        var list = await query.ToListAsync();
        return list.Select(MapToReadDto);
    }

    // READ / GET BY ID
    public async Task<TransactionReadDto?> GetByIdAsync(int id)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.TransactionID == id);

        return transaction == null ? null : MapToReadDto(transaction);
    }

    // ADD / CREATE
    public async Task<TransactionReadDto> CreateAsync(TransactionCreateDto dto)
    {
        var entity = new Transaction
        {
            Name = dto.Name,
            TotalAmount = dto.TotalAmount,
            Type = dto.Type,
            Status = TransactionStatus.Active,
            CategoryID = dto.CategoryID,
            PaymentMethod = dto.PaymentMethod,
            IsInstallment = dto.IsInstallment,
            TotalInstallments = dto.IsInstallment ? Math.Max(1, dto.TotalInstallments) : 1,
            IsRecurrent = dto.IsRecurrent,
            RecurrenceInterval = dto.IsRecurrent ? dto.RecurrenceInterval : RecurrenceInterval.None,
            RecurrenceTargetDay = dto.IsRecurrent ? dto.FirstDueDate.Day : null,
            UserID = dto.UserID,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Automatically build child Expenses based on rule parameters
        GenerateExpensesForTransaction(entity, dto.FirstDueDate);

        _context.Transactions.Add(entity);
        await _context.SaveChangesAsync();

        // Re-load Category for complete Read DTO response
        await _context.Entry(entity).Reference(e => e.Category).LoadAsync();

        return MapToReadDto(entity);
    }

    // EDIT / UPDATE (Header metadata update)
    public async Task<bool> UpdateAsync(int id, TransactionUpdateDto dto)
    {
        var entity = await _context.Transactions
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.TransactionID == id);

        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.Type = dto.Type;
        entity.Status = dto.Status;
        entity.CategoryID = dto.CategoryID;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        // If transaction is cancelled, cascade status to all pending expenses
        if (dto.Status == TransactionStatus.Cancelled)
        {
            foreach (var expense in entity.Expenses.Where(e => e.Status == ExpenseStatus.Pending))
            {
                expense.Status = ExpenseStatus.Cancelled;
                expense.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // DELETE
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Transactions.FindAsync(id);
        if (entity == null) return false;

        // EF Core handles cascading delete to Expenses automatically via Foreign Key
        _context.Transactions.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    // --- DOMAIN GENERATION LOGIC ---

    private static void GenerateExpensesForTransaction(Transaction transaction, DateTime firstDueDate)
    {
        if (transaction.IsInstallment && transaction.TotalInstallments > 1)
        {
            GenerateInstallmentExpenses(transaction, firstDueDate);
        }
        else
        {
            // Single purchase or first cycle of a recurrent bill
            var expense = new Expense
            {
                Amount = transaction.TotalAmount,
                PaidAmount = transaction.PaymentMethod == PaymentMethod.CreditCard ? 0.00m : transaction.TotalAmount,
                DueDate = firstDueDate,
                PaidDate = transaction.PaymentMethod == PaymentMethod.CreditCard ? null : DateTime.UtcNow,
                CurrentInstallment = 1,
                Status = transaction.PaymentMethod == PaymentMethod.CreditCard ? ExpenseStatus.Pending : ExpenseStatus.Paid,
                UserID = transaction.UserID,
                CreatedAtUtc = DateTime.UtcNow
            };

            transaction.Expenses.Add(expense);
        }
    }

    private static void GenerateInstallmentExpenses(Transaction transaction, DateTime startDate)
    {
        int count = transaction.TotalInstallments;
        
        // Cent-rounding math fix
        decimal baseAmount = Math.Floor((transaction.TotalAmount / count) * 100m) / 100m;
        decimal remainder = transaction.TotalAmount - (baseAmount * count);

        int targetDay = startDate.Day;

        for (int i = 1; i <= count; i++)
        {
            // Add remainder cents to the very first installment line
            decimal installmentAmount = (i == 1) ? baseAmount + remainder : baseAmount;
            
            // Calculate date with date-drift safety
            DateTime dueDate = CalculateNextDate(startDate, i - 1, transaction.RecurrenceInterval, targetDay);

            var expense = new Expense
            {
                Amount = installmentAmount,
                PaidAmount = 0.00m,
                DueDate = dueDate,
                CurrentInstallment = i,
                Status = ExpenseStatus.Pending,
                UserID = transaction.UserID,
                CreatedAtUtc = DateTime.UtcNow
            };

            transaction.Expenses.Add(expense);
        }
    }

    private static DateTime CalculateNextDate(DateTime baseDate, int monthOffset, RecurrenceInterval interval, int targetDay)
    {
        if (monthOffset == 0) return baseDate;

        return interval switch
        {
            RecurrenceInterval.Weekly => baseDate.AddDays(7 * monthOffset),
            RecurrenceInterval.Yearly => baseDate.AddYears(monthOffset),
            _ => AddMonthsSafely(baseDate, monthOffset, targetDay) // Monthly default
        };
    }

    private static DateTime AddMonthsSafely(DateTime date, int monthsToAdd, int targetDay)
    {
        var targetMonth = date.AddMonths(monthsToAdd);
        int daysInMonth = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);
        int clampedDay = Math.Min(targetDay, daysInMonth);

        return new DateTime(targetMonth.Year, targetMonth.Month, clampedDay, date.Hour, date.Minute, date.Second, date.Kind);
    }

    // Mapping Helper
    private static TransactionReadDto MapToReadDto(Transaction t)
    {
        return new TransactionReadDto
        {
            TransactionID = t.TransactionID,
            Name = t.Name,
            TotalAmount = t.TotalAmount,
            Type = t.Type,
            Status = t.Status,
            CategoryID = t.CategoryID,
            CategoryName = t.Category?.Name ?? "Uncategorized",
            PaymentMethod = t.PaymentMethod,
            IsInstallment = t.IsInstallment,
            TotalInstallments = t.TotalInstallments,
            IsRecurrent = t.IsRecurrent,
            RecurrenceInterval = t.RecurrenceInterval,
            RecurrenceTargetDay = t.RecurrenceTargetDay,
            UserID = t.UserID,
            CreatedAtUtc = t.CreatedAtUtc,
            Expenses = t.Expenses.Select(e => new ExpenseReadDto
            {
                ExpenseID = e.ExpenseID,
                TransactionID = e.TransactionID,
                Amount = e.Amount,
                PaidAmount = e.PaidAmount,
                DueDate = e.DueDate,
                PaidDate = e.PaidDate,
                CurrentInstallment = e.CurrentInstallment,
                Status = e.Status
            }).OrderBy(e => e.CurrentInstallment).ToList()
        };
    }
}