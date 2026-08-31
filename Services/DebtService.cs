using FinTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class DebtService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DebtService> _logger;

    public DebtService(AppDbContext context, ILogger<DebtService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DebtSummaryDto> GetSummaryAsync(int userId)
    {
        var debts = await _context.Debts
            .Include(d => d.Payments)
            .Where(d => d.UserID == userId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync();

        var dtos = debts.Select(MapToReadDto).ToList();

        var activeDebts = dtos.Where(d => !d.IsPaidOff).ToList();
        var totalOriginal = dtos.Sum(d => d.OriginalPrincipal);
        var totalRemaining = activeDebts.Sum(d => d.RemainingBalance);
        var totalPaid = dtos.Sum(d => d.TotalPaidAmount);
        var overallProgress = totalOriginal > 0
            ? Math.Round(((totalOriginal - totalRemaining) / totalOriginal) * 100, 2)
            : 100m;

        // Calculate estimated monthly obligation
        decimal totalMonthlyObligation = 0;
        foreach (var d in activeDebts)
        {
            if (d.InstallmentAmount.HasValue && d.InstallmentAmount.Value > 0)
            {
                totalMonthlyObligation += d.PaymentFrequency switch
                {
                    RecurrenceInterval.Weekly => d.InstallmentAmount.Value * 4.33m,
                    RecurrenceInterval.Daily => d.InstallmentAmount.Value * 30m,
                    RecurrenceInterval.Yearly => d.InstallmentAmount.Value / 12m,
                    _ => d.InstallmentAmount.Value
                };
            }
            else if (d.TotalInstallments.HasValue && d.TotalInstallments.Value > 0 && d.RemainingBalance > 0)
            {
                var remainingInst = Math.Max(1, d.TotalInstallments.Value - d.PaidInstallments);
                totalMonthlyObligation += d.RemainingBalance / remainingInst;
            }
        }

        // Calculate weighted average interest rate for active debts
        decimal weightedRate = 0;
        if (totalRemaining > 0)
        {
            var weightedSum = activeDebts.Sum(d => d.RemainingBalance * d.InterestRate);
            weightedRate = Math.Round(weightedSum / totalRemaining, 2);
        }

        return new DebtSummaryDto
        {
            TotalOriginalPrincipal = totalOriginal,
            TotalRemainingBalance = totalRemaining,
            TotalPaidAmount = totalPaid,
            OverallProgressPercentage = overallProgress,
            TotalMonthlyObligation = Math.Round(totalMonthlyObligation, 2),
            WeightedAverageInterestRate = weightedRate,
            ActiveDebtsCount = activeDebts.Count,
            PaidOffDebtsCount = dtos.Count(d => d.IsPaidOff),
            Debts = dtos
        };
    }

    public async Task<DebtReadDto?> GetByIdAsync(int id, int userId)
    {
        var debt = await _context.Debts
            .Include(d => d.Payments)
            .FirstOrDefaultAsync(d => d.DebtID == id && d.UserID == userId);

        if (debt == null) return null;
        return MapToReadDto(debt);
    }

    public async Task<DebtReadDto> CreateAsync(DebtCreateDto dto)
    {
        var remaining = dto.RemainingBalance ?? dto.OriginalPrincipal;

        var debt = new Debt
        {
            UserID = dto.UserID,
            Name = dto.Name.Trim(),
            Issuer = dto.Issuer.Trim(),
            DebtType = dto.DebtType,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim(),
            OriginalPrincipal = dto.OriginalPrincipal,
            RemainingBalance = remaining,
            InterestRate = dto.InterestRate,
            RateType = dto.RateType,
            PaymentFrequency = dto.PaymentFrequency,
            InstallmentAmount = dto.InstallmentAmount,
            TotalInstallments = dto.TotalInstallments,
            PaidInstallments = dto.PaidInstallments,
            StartDate = dto.StartDate,
            DueDay = dto.DueDay ?? (dto.StartDate.Day <= 28 ? dto.StartDate.Day : 28),
            MaturityDate = dto.MaturityDate,
            IsPaidOff = remaining <= 0,
            AutoGenerateExpenses = dto.AutoGenerateExpenses,
            Description = dto.Description,
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        // If AutoGenerateExpenses is requested, automatically link or create a Transaction + Expenses
        if (dto.AutoGenerateExpenses && !debt.IsPaidOff)
        {
            try
            {
                var transaction = await GenerateLinkedTransactionAsync(debt);
                if (transaction != null)
                {
                    debt.Transaction = transaction;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-generate linked expenses for debt {DebtName}", debt.Name);
            }
        }

        _context.Debts.Add(debt);
        await _context.SaveChangesAsync();

        return MapToReadDto(debt);
    }

    public async Task<bool> UpdateAsync(int id, int userId, DebtUpdateDto dto)
    {
        var debt = await _context.Debts
            .Include(d => d.Transaction)
                .ThenInclude(t => t!.Expenses)
            .FirstOrDefaultAsync(d => d.DebtID == id && d.UserID == userId);

        if (debt == null) return false;

        debt.Name = dto.Name.Trim();
        debt.Issuer = dto.Issuer.Trim();
        debt.DebtType = dto.DebtType;
        debt.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim();
        if (dto.RemainingBalance.HasValue)
        {
            debt.RemainingBalance = dto.RemainingBalance.Value;
        }
        debt.InterestRate = dto.InterestRate;
        debt.RateType = dto.RateType;
        debt.PaymentFrequency = dto.PaymentFrequency;
        debt.InstallmentAmount = dto.InstallmentAmount;
        debt.TotalInstallments = dto.TotalInstallments;
        debt.DueDay = dto.DueDay;
        debt.MaturityDate = dto.MaturityDate;
        debt.IsPaidOff = dto.IsPaidOff || debt.RemainingBalance <= 0;
        debt.Description = dto.Description;
        debt.LastUpdatedUtc = DateTime.UtcNow;

        if (debt.Transaction != null)
        {
            debt.Transaction.Name = $"[Loan] {debt.Name} ({debt.Issuer})";
            debt.Transaction.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecordPaymentAsync(int id, int userId, DebtPaymentCreateDto dto)
    {
        var debt = await _context.Debts
            .Include(d => d.Payments)
            .Include(d => d.Transaction)
                .ThenInclude(t => t!.Expenses)
            .FirstOrDefaultAsync(d => d.DebtID == id && d.UserID == userId);

        if (debt == null) return false;

        var payment = new DebtPayment
        {
            DebtID = debt.DebtID,
            Amount = dto.Amount,
            PrincipalAmount = dto.PrincipalAmount ?? dto.Amount,
            InterestAmount = dto.InterestAmount ?? 0,
            PaymentDate = dto.PaymentDate,
            ExpenseID = dto.ExpenseID,
            Notes = dto.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };

        debt.Payments.Add(payment);

        // Reduce remaining balance by the principal paid
        var principalPaid = payment.PrincipalAmount ?? payment.Amount;
        debt.RemainingBalance = Math.Max(0, debt.RemainingBalance - principalPaid);
        debt.PaidInstallments += 1;

        if (debt.RemainingBalance <= 0)
        {
            debt.IsPaidOff = true;
        }

        debt.LastUpdatedUtc = DateTime.UtcNow;

        // If there's an active linked expense pending for this debt/date, mark it paid
        if (debt.Transaction?.Expenses != null && debt.Transaction.Expenses.Any())
        {
            var targetExpense = dto.ExpenseID.HasValue
                ? debt.Transaction.Expenses.FirstOrDefault(e => e.ExpenseID == dto.ExpenseID.Value)
                : debt.Transaction.Expenses
                    .Where(e => e.Status == ExpenseStatus.Pending || e.Status == ExpenseStatus.Overdue)
                    .OrderBy(e => e.DueDate)
                    .FirstOrDefault();

            if (targetExpense != null)
            {
                targetExpense.PaidAmount = dto.Amount;
                targetExpense.PaidDate = dto.PaymentDate;
                targetExpense.Status = ExpenseStatus.Paid;
                targetExpense.UpdatedAtUtc = DateTime.UtcNow;
                payment.ExpenseID = targetExpense.ExpenseID;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PayoffDebtAsync(int id, int userId)
    {
        var debt = await _context.Debts
            .Include(d => d.Payments)
            .FirstOrDefaultAsync(d => d.DebtID == id && d.UserID == userId);

        if (debt == null) return false;

        if (debt.RemainingBalance > 0)
        {
            debt.Payments.Add(new DebtPayment
            {
                DebtID = debt.DebtID,
                Amount = debt.RemainingBalance,
                PrincipalAmount = debt.RemainingBalance,
                InterestAmount = 0,
                PaymentDate = DateTime.UtcNow,
                Notes = "Full debt payoff settlement",
                CreatedAtUtc = DateTime.UtcNow
            });

            debt.RemainingBalance = 0;
        }

        debt.IsPaidOff = true;
        debt.LastUpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var debt = await _context.Debts
            .Include(d => d.Payments)
            .Include(d => d.Transaction)
                .ThenInclude(t => t!.Expenses)
            .FirstOrDefaultAsync(d => d.DebtID == id && d.UserID == userId);

        if (debt == null) return false;

        if (debt.Transaction != null)
        {
            _context.Transactions.Remove(debt.Transaction);
        }

        _context.Debts.Remove(debt);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DebtScheduleItemDto>> GetScheduleAsync(int id, int userId)
    {
        var debt = await _context.Debts
            .Include(d => d.Payments)
            .FirstOrDefaultAsync(d => d.DebtID == id && d.UserID == userId);

        if (debt == null) return new List<DebtScheduleItemDto>();

        var schedule = new List<DebtScheduleItemDto>();

        var totalInst = debt.TotalInstallments ?? 12;
        var start = debt.StartDate;
        var dueDay = debt.DueDay ?? (start.Day <= 28 ? start.Day : 28);
        var currentBalance = debt.OriginalPrincipal;

        decimal periodicRate = (debt.InterestRate / 100m);
        if (debt.RateType == DebtRateType.FixedAnnual || debt.RateType == DebtRateType.CDI_Linked || debt.RateType == DebtRateType.IPCA_Linked)
        {
            // Monthly compounding approximation
            periodicRate = (decimal)(Math.Pow((double)(1 + periodicRate), 1.0 / 12.0) - 1.0);
        }

        decimal paymentAmount = debt.InstallmentAmount ?? 0;
        if (paymentAmount <= 0)
        {
            if (periodicRate > 0)
            {
                var r = (double)periodicRate;
                var n = totalInst;
                var p = (double)debt.OriginalPrincipal;
                var pmt = p * (r * Math.Pow(1 + r, n)) / (Math.Pow(1 + r, n) - 1);
                paymentAmount = Math.Round((decimal)pmt, 2);
            }
            else
            {
                paymentAmount = Math.Round(debt.OriginalPrincipal / totalInst, 2);
            }
        }

        for (int i = 1; i <= totalInst; i++)
        {
            var dueDate = new DateTime(start.Year, start.Month, 1).AddMonths(i - 1);
            var daysInMonth = DateTime.DaysInMonth(dueDate.Year, dueDate.Month);
            var actualDueDay = Math.Min(dueDay, daysInMonth);
            dueDate = new DateTime(dueDate.Year, dueDate.Month, actualDueDay, 0, 0, 0, DateTimeKind.Utc);

            var interestPortion = Math.Round(currentBalance * periodicRate, 2);
            var principalPortion = Math.Min(currentBalance, Math.Max(0, paymentAmount - interestPortion));
            var remainingAfter = Math.Max(0, currentBalance - principalPortion);

            schedule.Add(new DebtScheduleItemDto
            {
                InstallmentNumber = i,
                DueDate = dueDate,
                ScheduledPayment = paymentAmount,
                PrincipalPortion = principalPortion,
                InterestPortion = interestPortion,
                RemainingBalanceAfter = remainingAfter,
                IsPaid = i <= debt.PaidInstallments || (debt.IsPaidOff && remainingAfter >= debt.RemainingBalance)
            });

            currentBalance = remainingAfter;
            if (currentBalance <= 0) break;
        }

        return schedule;
    }

    private async Task<Transaction?> GenerateLinkedTransactionAsync(Debt debt)
    {
        // Find or create category for Loans / Debts
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.UserID == debt.UserID && (c.Name.ToLower() == "loans" || c.Name.ToLower() == "debts" || c.Name.ToLower() == "empréstimos"));

        if (category == null)
        {
            category = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserID == debt.UserID);
        }

        int categoryId = category?.CategoryID ?? 1;

        var installmentAmount = debt.InstallmentAmount ?? (debt.TotalInstallments.HasValue && debt.TotalInstallments.Value > 0
            ? Math.Round(debt.OriginalPrincipal / debt.TotalInstallments.Value, 2)
            : debt.OriginalPrincipal);

        var totalInstallments = debt.TotalInstallments ?? (debt.PaymentFrequency != RecurrenceInterval.None ? 12 : 1);
        var isInstallment = totalInstallments > 1;

        var transaction = new Transaction
        {
            UserID = debt.UserID,
            Name = $"[Loan] {debt.Name} ({debt.Issuer})",
            Description = $"Automatic loan expense for {debt.Name} with rate {debt.InterestRate}%",
            TotalAmount = installmentAmount * totalInstallments,
            Type = TransactionType.Expense,
            Status = TransactionStatus.Active,
            PaymentMethod = PaymentMethod.BankTransfer,
            CategoryID = categoryId,
            IsInstallment = isInstallment,
            TotalInstallments = totalInstallments,
            IsRecurrent = !isInstallment && debt.PaymentFrequency != RecurrenceInterval.None,
            RecurrenceInterval = debt.PaymentFrequency,
            RecurrenceTargetDay = debt.DueDay,
            CreatedAtUtc = DateTime.UtcNow
        };

        var start = debt.StartDate;
        var dueDay = debt.DueDay ?? (start.Day <= 28 ? start.Day : 28);

        for (int i = 1; i <= totalInstallments; i++)
        {
            var dueDate = new DateTime(start.Year, start.Month, 1).AddMonths(i - 1);
            var daysInMonth = DateTime.DaysInMonth(dueDate.Year, dueDate.Month);
            var actualDueDay = Math.Min(dueDay, daysInMonth);
            dueDate = new DateTime(dueDate.Year, dueDate.Month, actualDueDay, 0, 0, 0, DateTimeKind.Utc);

            var isPaid = i <= debt.PaidInstallments;

            transaction.Expenses.Add(new Expense
            {
                Amount = installmentAmount,
                PaidAmount = isPaid ? installmentAmount : 0,
                PaidDate = isPaid ? dueDate : null,
                DueDate = dueDate,
                CurrentInstallment = i,
                Status = isPaid ? ExpenseStatus.Paid : (dueDate < DateTime.UtcNow ? ExpenseStatus.Overdue : ExpenseStatus.Pending),
                UserID = debt.UserID,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return transaction;
    }

    private static DebtReadDto MapToReadDto(Debt d)
    {
        return new DebtReadDto
        {
            DebtID = d.DebtID,
            UserID = d.UserID,
            Name = d.Name,
            Issuer = d.Issuer,
            DebtType = d.DebtType,
            Currency = d.Currency,
            OriginalPrincipal = d.OriginalPrincipal,
            RemainingBalance = d.RemainingBalance,
            TotalPaidAmount = d.TotalPaidAmount,
            ProgressPercentage = d.ProgressPercentage,
            InterestRate = d.InterestRate,
            RateType = d.RateType,
            PaymentFrequency = d.PaymentFrequency,
            InstallmentAmount = d.InstallmentAmount,
            TotalInstallments = d.TotalInstallments,
            PaidInstallments = d.PaidInstallments,
            StartDate = d.StartDate,
            DueDay = d.DueDay,
            MaturityDate = d.MaturityDate,
            IsPaidOff = d.IsPaidOff,
            AutoGenerateExpenses = d.AutoGenerateExpenses,
            TransactionID = d.TransactionID,
            Description = d.Description,
            CreatedAtUtc = d.CreatedAtUtc,
            LastUpdatedUtc = d.LastUpdatedUtc,
            Payments = d.Payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new DebtPaymentReadDto
                {
                    DebtPaymentID = p.DebtPaymentID,
                    DebtID = p.DebtID,
                    Amount = p.Amount,
                    PrincipalAmount = p.PrincipalAmount,
                    InterestAmount = p.InterestAmount,
                    PaymentDate = p.PaymentDate,
                    ExpenseID = p.ExpenseID,
                    Notes = p.Notes,
                    CreatedAtUtc = p.CreatedAtUtc
                }).ToList()
        };
    }
}
