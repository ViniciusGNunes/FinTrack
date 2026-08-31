using FinTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class TransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TransactionReadDto>> GetAllAsync(int? userId = null)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Category)
            .Include(t => t.Expenses)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserID == userId.Value);
        }

        var list = await query.ToListAsync();
        return list.Select(t => MapToReadDto(t));
    }

    public async Task<IEnumerable<TransactionReadDto>> GetByTimePeriodAsync(TimeCategory timeCategory, TimePeriod timePeriod, int? userId = null)
    {
        var (startDate, endDate) = CalculateDateRange(timeCategory, timePeriod);

        var query = _context.Transactions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Category)
            .Include(t => t.Expenses)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserID == userId.Value);
        }

        // Transactions that have:
        // 1. Existing expenses due in [startDate, endDate)
        // 2. Active recurring transactions whose firstDueDate <= endDate and cancellationDate >= startDate
        // 3. One-off transactions created in [startDate, endDate)
        query = query.Where(t =>
            t.Expenses.Any(e => e.DueDate >= startDate && e.DueDate < endDate) ||
            (t.IsRecurrent && t.Status == TransactionStatus.Active && (!t.CancellationDate.HasValue || t.CancellationDate.Value >= startDate)) ||
            (!t.Expenses.Any() && t.CreatedAtUtc >= startDate && t.CreatedAtUtc < endDate)
        );

        var list = await query.ToListAsync();
        var result = new List<TransactionReadDto>();

        foreach (var transaction in list)
        {
            var dto = MapToReadDto(transaction, startDate, endDate);
            // Only include if there are expenses in this period or the transaction itself falls in this period
            if (dto.Expenses.Any() || (!transaction.Expenses.Any() && transaction.CreatedAtUtc >= startDate && transaction.CreatedAtUtc < endDate))
            {
                result.Add(dto);
            }
        }

        return result;
    }

    public static (DateTime StartDate, DateTime EndDate) CalculateDateRange(TimeCategory category, TimePeriod period, DateTime? referenceDate = null)
    {
        var now = referenceDate ?? DateTime.UtcNow;
        var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        return period switch
        {
            TimePeriod.Day => category switch
            {
                TimeCategory.Last => (today.AddDays(-1), today),
                TimeCategory.Next => (today.AddDays(1), today.AddDays(2)),
                _ => (today, today.AddDays(1))
            },
            TimePeriod.Week => CalculateWeekRange(today, category, 1),
            TimePeriod.TwoWeeks => CalculateWeekRange(today, category, 2),
            TimePeriod.Month => category switch
            {
                TimeCategory.Last => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1), new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)),
                TimeCategory.Next => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1), new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(2)),
                _ => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1))
            },
            TimePeriod.Year => category switch
            {
                TimeCategory.Last => (new DateTime(today.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                TimeCategory.Next => (new DateTime(today.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(today.Year + 2, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                _ => (new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(today.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            },
            _ => (today, today.AddDays(1))
        };
    }

    private static (DateTime StartDate, DateTime EndDate) CalculateWeekRange(DateTime today, TimeCategory category, int weekMultiplier)
    {
        int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var startOfCurrentWeek = today.AddDays(-diff);
        int days = 7 * weekMultiplier;

        return category switch
        {
            TimeCategory.Last => (startOfCurrentWeek.AddDays(-days), startOfCurrentWeek),
            TimeCategory.Next => (startOfCurrentWeek.AddDays(days), startOfCurrentWeek.AddDays(days * 2)),
            _ => (startOfCurrentWeek, startOfCurrentWeek.AddDays(days))
        };
    }

    public async Task<TransactionReadDto?> GetByIdAsync(int id)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.TransactionID == id);

        return transaction == null ? null : MapToReadDto(transaction);
    }

    public async Task<TransactionReadDto> CreateAsync(TransactionCreateDto dto)
    {
        var entity = new Transaction
        {
            Name = dto.Name,
            Description = dto.Description,
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
            UserID = dto.UserID.GetValueOrDefault(),
            CreatedAtUtc = DateTime.UtcNow
        };

        GenerateExpensesForTransaction(entity, dto.FirstDueDate);

        _context.Transactions.Add(entity);
        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(e => e.Category).LoadAsync();

        return MapToReadDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, TransactionUpdateDto dto)
    {
        var entity = await _context.Transactions
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.TransactionID == id);

        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Type = dto.Type;
        entity.Status = dto.Status;
        entity.CategoryID = dto.CategoryID;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.CancellationDate = dto.CancellationDate;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (dto.TotalAmount.HasValue && dto.TotalAmount.Value > 0)
        {
            entity.TotalAmount = dto.TotalAmount.Value;

            // If it's a single expense or recurrent, sync pending expense amounts
            if (!entity.IsInstallment)
            {
                foreach (var exp in entity.Expenses.Where(e => e.Status == ExpenseStatus.Pending))
                {
                    exp.Amount = dto.TotalAmount.Value;
                    exp.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
            else
            {
                // Recalculate remaining pending installments
                var pendingExpenses = entity.Expenses
                    .Where(e => e.Status == ExpenseStatus.Pending)
                    .OrderBy(e => e.CurrentInstallment)
                    .ToList();

                var paidSum = entity.Expenses
                    .Where(e => e.Status == ExpenseStatus.Paid)
                    .Sum(e => e.Amount);

                var remainingAmount = Math.Max(0, entity.TotalAmount - paidSum);
                if (pendingExpenses.Count > 0)
                {
                    var baseAmt = Math.Floor((remainingAmount / pendingExpenses.Count) * 100m) / 100m;
                    var remainder = remainingAmount - (baseAmt * pendingExpenses.Count);

                    for (int i = 0; i < pendingExpenses.Count; i++)
                    {
                        pendingExpenses[i].Amount = (i == 0) ? baseAmt + remainder : baseAmt;
                        pendingExpenses[i].UpdatedAtUtc = DateTime.UtcNow;
                    }
                }
            }
        }

        // Regra de Cancelamento: Se a transação for cancelada, remove ou cancela expenses pendentes futuras
        if (dto.Status == TransactionStatus.Cancelled)
        {
            DateTime effectiveDate = dto.CancellationDate ?? DateTime.UtcNow;

            var pendingExpenses = entity.Expenses
                .Where(e => e.Status == ExpenseStatus.Pending && e.DueDate >= effectiveDate)
                .ToList();

            foreach (var expense in pendingExpenses)
            {
                expense.Status = ExpenseStatus.Cancelled;
                expense.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelSubscriptionAsync(int id, DateTime? cancellationDate = null)
    {
        var entity = await _context.Transactions
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.TransactionID == id);

        if (entity == null) return false;

        var effectiveDate = cancellationDate ?? DateTime.UtcNow;
        entity.Status = TransactionStatus.Cancelled;
        entity.CancellationDate = effectiveDate;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        var pendingExpenses = entity.Expenses
            .Where(e => e.Status == ExpenseStatus.Pending && e.DueDate >= effectiveDate)
            .ToList();

        foreach (var expense in pendingExpenses)
        {
            expense.Status = ExpenseStatus.Cancelled;
            expense.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Transactions.FindAsync(id);
        if (entity == null) return false;

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
            bool isCreditCard = transaction.PaymentMethod == PaymentMethod.CreditCard;

            var expense = new Expense
            {
                Amount = transaction.TotalAmount,
                PaidAmount = isCreditCard ? 0.00m : transaction.TotalAmount,
                DueDate = firstDueDate,
                PaidDate = isCreditCard ? null : DateTime.UtcNow,
                CurrentInstallment = 1,
                Status = isCreditCard ? ExpenseStatus.Pending : ExpenseStatus.Paid,
                UserID = transaction.UserID,
                CreatedAtUtc = DateTime.UtcNow
            };

            transaction.Expenses.Add(expense);
        }
    }

    private static void GenerateInstallmentExpenses(Transaction transaction, DateTime startDate)
    {
        int count = transaction.TotalInstallments;

        decimal baseAmount = Math.Floor((transaction.TotalAmount / count) * 100m) / 100m;
        decimal remainder = transaction.TotalAmount - (baseAmount * count);

        int targetDay = startDate.Day;

        for (int i = 1; i <= count; i++)
        {
            decimal installmentAmount = (i == 1) ? baseAmount + remainder : baseAmount;
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
            _ => AddMonthsSafely(baseDate, monthOffset, targetDay)
        };
    }

    private static DateTime AddMonthsSafely(DateTime date, int monthsToAdd, int targetDay)
    {
        var targetMonth = date.AddMonths(monthsToAdd);
        int daysInMonth = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);
        int clampedDay = Math.Min(targetDay, daysInMonth);

        return new DateTime(targetMonth.Year, targetMonth.Month, clampedDay, date.Hour, date.Minute, date.Second, date.Kind);
    }

    private static TransactionReadDto MapToReadDto(Transaction t, DateTime? startDate = null, DateTime? endDate = null)
    {
        var expensesList = new List<ExpenseReadDto>();

        if (startDate.HasValue && endDate.HasValue)
        {
            var matchingExpenses = t.Expenses
                .Where(e => e.DueDate >= startDate.Value && e.DueDate < endDate.Value)
                .Select(MapToExpenseReadDto)
                .ToList();

            expensesList.AddRange(matchingExpenses);

            // If it's an active recurrent transaction, project occurrence if not already present in DB for this period
            if (t.IsRecurrent && t.Status == TransactionStatus.Active && (!t.CancellationDate.HasValue || t.CancellationDate.Value >= startDate.Value))
            {
                ProjectRecurringExpensesForWindow(t, startDate.Value, endDate.Value, expensesList);
            }
        }
        else
        {
            expensesList = t.Expenses.Select(MapToExpenseReadDto).OrderBy(e => e.CurrentInstallment).ToList();
        }

        return new TransactionReadDto
        {
            TransactionID = t.TransactionID,
            Name = t.Name,
            Description = t.Description,
            TotalAmount = t.TotalAmount,
            RefundedAmount = t.RefundedAmount,
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
            CancellationDate = t.CancellationDate,
            UserID = t.UserID,
            CreatedAtUtc = t.CreatedAtUtc,
            Expenses = expensesList.OrderBy(e => e.DueDate).ToList()
        };
    }

    private static ExpenseReadDto MapToExpenseReadDto(Expense e)
    {
        return new ExpenseReadDto
        {
            ExpenseID = e.ExpenseID,
            TransactionID = e.TransactionID,
            Amount = e.Amount,
            PaidAmount = e.PaidAmount,
            RefundedAmount = e.RefundedAmount,
            DueDate = e.DueDate,
            PaidDate = e.PaidDate,
            RefundDate = e.RefundDate,
            RefundReason = e.RefundReason,
            CurrentInstallment = e.CurrentInstallment,
            Status = e.Status
        };
    }

    private static void ProjectRecurringExpensesForWindow(
        Transaction transaction,
        DateTime windowStart,
        DateTime windowEnd,
        List<ExpenseReadDto> existingExpenses)
    {
        var firstExpense = transaction.Expenses.OrderBy(e => e.DueDate).FirstOrDefault();
        var originDate = firstExpense?.DueDate ?? transaction.CreatedAtUtc;
        var targetDay = transaction.RecurrenceTargetDay ?? originDate.Day;
        var maxDate = transaction.CancellationDate ?? DateTime.MaxValue;

        switch (transaction.RecurrenceInterval)
        {
            case RecurrenceInterval.Monthly:
                // Check every month that intersects [windowStart, windowEnd)
                var currentMonth = new DateTime(windowStart.Year, windowStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endMonth = new DateTime(windowEnd.Year, windowEnd.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

                while (currentMonth < endMonth)
                {
                    var occurrence = AddMonthsSafely(currentMonth, 0, targetDay);
                    if (occurrence >= windowStart && occurrence < windowEnd && occurrence >= originDate && occurrence < maxDate)
                    {
                        if (!existingExpenses.Any(e => e.DueDate.Date == occurrence.Date))
                        {
                            existingExpenses.Add(new ExpenseReadDto
                            {
                                ExpenseID = 0,
                                TransactionID = transaction.TransactionID,
                                Amount = transaction.TotalAmount,
                                PaidAmount = 0.00m,
                                RefundedAmount = 0.00m,
                                DueDate = occurrence,
                                CurrentInstallment = 1,
                                Status = ExpenseStatus.Pending
                            });
                        }
                    }
                    currentMonth = currentMonth.AddMonths(1);
                }
                break;

            case RecurrenceInterval.Weekly:
                var checkDate = originDate;
                while (checkDate < windowStart)
                {
                    checkDate = checkDate.AddDays(7);
                }
                while (checkDate < windowEnd && checkDate < maxDate)
                {
                    if (checkDate >= windowStart && !existingExpenses.Any(e => e.DueDate.Date == checkDate.Date))
                    {
                        existingExpenses.Add(new ExpenseReadDto
                        {
                            ExpenseID = 0,
                            TransactionID = transaction.TransactionID,
                            Amount = transaction.TotalAmount,
                            PaidAmount = 0.00m,
                            RefundedAmount = 0.00m,
                            DueDate = checkDate,
                            CurrentInstallment = 1,
                            Status = ExpenseStatus.Pending
                        });
                    }
                    checkDate = checkDate.AddDays(7);
                }
                break;

            case RecurrenceInterval.Daily:
                var dayCheck = windowStart.Date;
                while (dayCheck < windowEnd.Date && dayCheck < maxDate.Date)
                {
                    if (dayCheck >= originDate.Date && !existingExpenses.Any(e => e.DueDate.Date == dayCheck))
                    {
                        existingExpenses.Add(new ExpenseReadDto
                        {
                            ExpenseID = 0,
                            TransactionID = transaction.TransactionID,
                            Amount = transaction.TotalAmount,
                            PaidAmount = 0.00m,
                            RefundedAmount = 0.00m,
                            DueDate = dayCheck,
                            CurrentInstallment = 1,
                            Status = ExpenseStatus.Pending
                        });
                    }
                    dayCheck = dayCheck.AddDays(1);
                }
                break;

            case RecurrenceInterval.Yearly:
                var currentYear = windowStart.Year;
                while (currentYear <= windowEnd.Year)
                {
                    try
                    {
                        var occurrence = new DateTime(currentYear, originDate.Month, Math.Min(targetDay, DateTime.DaysInMonth(currentYear, originDate.Month)), originDate.Hour, originDate.Minute, originDate.Second, DateTimeKind.Utc);
                        if (occurrence >= windowStart && occurrence < windowEnd && occurrence >= originDate && occurrence < maxDate)
                        {
                            if (!existingExpenses.Any(e => e.DueDate.Date == occurrence.Date))
                            {
                                existingExpenses.Add(new ExpenseReadDto
                                {
                                    ExpenseID = 0,
                                    TransactionID = transaction.TransactionID,
                                    Amount = transaction.TotalAmount,
                                    PaidAmount = 0.00m,
                                    RefundedAmount = 0.00m,
                                    DueDate = occurrence,
                                    CurrentInstallment = 1,
                                    Status = ExpenseStatus.Pending
                                });
                            }
                        }
                    }
                    catch { }
                    currentYear++;
                }
                break;
        }
    }
}