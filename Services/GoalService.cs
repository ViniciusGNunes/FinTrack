using FinTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class GoalService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GoalService> _logger;

    public GoalService(AppDbContext context, ILogger<GoalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GoalSummaryDto> GetSummaryAsync(int userId)
    {
        var goals = await _context.Goals
            .Include(g => g.LinkedDebt)
            .Include(g => g.LinkedCategory)
            .Where(g => g.UserID == userId)
            .OrderByDescending(g => g.CreatedAtUtc)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        // Preload monthly actuals for auto tracking
        var monthlyInvestments = await _context.InvestmentTransactions
            .Where(t => t.Investment!.UserID == userId && t.TransactionType == InvestmentTransactionType.Buy && t.TransactionDate >= startOfMonth && t.TransactionDate < endOfMonth)
            .SumAsync(t => t.Amount);

        var monthlyDebtPayments = await _context.DebtPayments
            .Where(p => p.Debt!.UserID == userId && p.PaymentDate >= startOfMonth && p.PaymentDate < endOfMonth)
            .SumAsync(p => p.Amount);

        var totalPortfolioValue = await _context.Investments
            .Where(i => i.UserID == userId && !i.IsLiquidated)
            .SumAsync(i => i.CurrentValue);

        var monthlyExpenses = await _context.Expenses
            .Where(e => e.UserID == userId && e.DueDate >= startOfMonth && e.DueDate < endOfMonth && e.Status != ExpenseStatus.Cancelled)
            .ToListAsync();

        var dtos = new List<GoalReadDto>();
        decimal monthlyInvestTarget = 0;
        decimal monthlyInvestActual = 0;
        decimal monthlyDebtTarget = 0;
        decimal monthlyDebtActual = 0;

        foreach (var goal in goals)
        {
            decimal currentAmt = goal.CurrentAmount;

            if (goal.AutoTrack)
            {
                switch (goal.Category)
                {
                    case GoalCategory.MonthlyInvestment:
                        currentAmt = monthlyInvestments;
                        monthlyInvestTarget += goal.TargetAmount;
                        monthlyInvestActual = monthlyInvestments;
                        break;

                    case GoalCategory.MonthlyDebtReduction:
                        if (goal.LinkedDebtID.HasValue)
                        {
                            var specificDebtPaid = await _context.DebtPayments
                                .Where(p => p.DebtID == goal.LinkedDebtID.Value && p.PaymentDate >= startOfMonth && p.PaymentDate < endOfMonth)
                                .SumAsync(p => p.Amount);
                            currentAmt = specificDebtPaid;
                        }
                        else
                        {
                            currentAmt = monthlyDebtPayments;
                        }
                        monthlyDebtTarget += goal.TargetAmount;
                        monthlyDebtActual = monthlyDebtPayments;
                        break;

                    case GoalCategory.ExpenseCap:
                        if (goal.LinkedCategoryID.HasValue)
                        {
                            // Filter expenses for specific category
                            var catExpenses = await _context.Expenses
                                .Include(e => e.Transaction)
                                .Where(e => e.UserID == userId && e.DueDate >= startOfMonth && e.DueDate < endOfMonth && e.Transaction!.CategoryID == goal.LinkedCategoryID.Value)
                                .SumAsync(e => e.Amount);
                            currentAmt = catExpenses;
                        }
                        else
                        {
                            currentAmt = monthlyExpenses.Sum(e => e.Amount);
                        }
                        break;

                    case GoalCategory.PortfolioMilestone:
                        currentAmt = totalPortfolioValue;
                        break;

                    case GoalCategory.TargetSavings:
                        // Target savings uses stored manual progress unless linked
                        break;
                }
            }

            var isCompleted = goal.Category == GoalCategory.ExpenseCap
                ? currentAmt <= goal.TargetAmount
                : currentAmt >= goal.TargetAmount;

            var progressPct = goal.TargetAmount > 0
                ? Math.Round((currentAmt / goal.TargetAmount) * 100, 2)
                : 100m;

            var remaining = Math.Max(0, goal.TargetAmount - currentAmt);

            // Compute pacing status
            string pacing = "OnTrack";
            var dayOfMonth = now.Day;
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var monthProgressPct = (decimal)dayOfMonth / daysInMonth * 100m;

            if (goal.Category == GoalCategory.ExpenseCap)
            {
                if (currentAmt > goal.TargetAmount) pacing = "OverBudget";
                else if (progressPct > monthProgressPct + 15) pacing = "BehindPace"; // Spending faster than month progression
                else pacing = "OnTrack";
            }
            else
            {
                if (isCompleted) pacing = "Achieved";
                else if (progressPct < monthProgressPct - 20) pacing = "BehindPace";
                else pacing = "OnTrack";
            }

            dtos.Add(new GoalReadDto
            {
                GoalID = goal.GoalID,
                UserID = goal.UserID,
                Title = goal.Title,
                Description = goal.Description,
                Category = goal.Category,
                Frequency = goal.Frequency,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = currentAmt,
                ProgressPercentage = progressPct,
                RemainingAmount = remaining,
                Currency = goal.Currency,
                LinkedDebtID = goal.LinkedDebtID,
                LinkedDebtName = goal.LinkedDebt?.Name,
                LinkedCategoryID = goal.LinkedCategoryID,
                LinkedCategoryName = goal.LinkedCategory?.Name,
                TargetDate = goal.TargetDate,
                AutoTrack = goal.AutoTrack,
                IsCompleted = isCompleted,
                PacingStatus = pacing,
                CreatedAtUtc = goal.CreatedAtUtc,
                LastUpdatedUtc = goal.LastUpdatedUtc
            });
        }

        var completedCount = dtos.Count(g => g.IsCompleted);
        var overallProgress = dtos.Any()
            ? Math.Round(dtos.Average(g => Math.Min(100, g.ProgressPercentage)), 2)
            : 0m;

        return new GoalSummaryDto
        {
            TotalGoalsCount = dtos.Count,
            ActiveGoalsCount = dtos.Count(g => !g.IsCompleted),
            CompletedGoalsCount = completedCount,
            MonthlyInvestmentTarget = monthlyInvestTarget,
            MonthlyInvestmentActual = monthlyInvestActual,
            MonthlyDebtReductionTarget = monthlyDebtTarget,
            MonthlyDebtReductionActual = monthlyDebtActual,
            OverallProgressPercentage = overallProgress,
            Goals = dtos
        };
    }

    public async Task<GoalReadDto?> GetByIdAsync(int id, int userId)
    {
        var goal = await _context.Goals
            .Include(g => g.LinkedDebt)
            .Include(g => g.LinkedCategory)
            .FirstOrDefaultAsync(g => g.GoalID == id && g.UserID == userId);

        if (goal == null) return null;

        return new GoalReadDto
        {
            GoalID = goal.GoalID,
            UserID = goal.UserID,
            Title = goal.Title,
            Description = goal.Description,
            Category = goal.Category,
            Frequency = goal.Frequency,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            ProgressPercentage = goal.ProgressPercentage,
            RemainingAmount = goal.RemainingAmount,
            Currency = goal.Currency,
            LinkedDebtID = goal.LinkedDebtID,
            LinkedDebtName = goal.LinkedDebt?.Name,
            LinkedCategoryID = goal.LinkedCategoryID,
            LinkedCategoryName = goal.LinkedCategory?.Name,
            TargetDate = goal.TargetDate,
            AutoTrack = goal.AutoTrack,
            IsCompleted = goal.IsCompleted,
            CreatedAtUtc = goal.CreatedAtUtc,
            LastUpdatedUtc = goal.LastUpdatedUtc
        };
    }

    public async Task<GoalReadDto> CreateAsync(GoalCreateDto dto)
    {
        var entity = new Goal
        {
            UserID = dto.UserID,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Category = dto.Category,
            Frequency = dto.Frequency,
            TargetAmount = dto.TargetAmount,
            CurrentAmount = dto.InitialAmount ?? 0,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim(),
            LinkedDebtID = dto.LinkedDebtID,
            LinkedCategoryID = dto.LinkedCategoryID,
            TargetDate = dto.TargetDate,
            AutoTrack = dto.AutoTrack,
            IsCompleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        _context.Goals.Add(entity);
        await _context.SaveChangesAsync();

        return new GoalReadDto
        {
            GoalID = entity.GoalID,
            UserID = entity.UserID,
            Title = entity.Title,
            Description = entity.Description,
            Category = entity.Category,
            Frequency = entity.Frequency,
            TargetAmount = entity.TargetAmount,
            CurrentAmount = entity.CurrentAmount,
            ProgressPercentage = entity.ProgressPercentage,
            RemainingAmount = entity.RemainingAmount,
            Currency = entity.Currency,
            LinkedDebtID = entity.LinkedDebtID,
            LinkedCategoryID = entity.LinkedCategoryID,
            TargetDate = entity.TargetDate,
            AutoTrack = entity.AutoTrack,
            IsCompleted = entity.IsCompleted,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastUpdatedUtc = entity.LastUpdatedUtc
        };
    }

    public async Task<bool> UpdateAsync(int id, int userId, GoalUpdateDto dto)
    {
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.GoalID == id && g.UserID == userId);

        if (goal == null) return false;

        goal.Title = dto.Title.Trim();
        goal.Description = dto.Description?.Trim();
        goal.Category = dto.Category;
        goal.Frequency = dto.Frequency;
        goal.TargetAmount = dto.TargetAmount;
        if (dto.CurrentAmount.HasValue)
        {
            goal.CurrentAmount = dto.CurrentAmount.Value;
        }
        goal.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "BRL" : dto.Currency.ToUpper().Trim();
        goal.LinkedDebtID = dto.LinkedDebtID;
        goal.LinkedCategoryID = dto.LinkedCategoryID;
        goal.TargetDate = dto.TargetDate;
        goal.AutoTrack = dto.AutoTrack;
        goal.IsCompleted = dto.IsCompleted;
        goal.LastUpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LogProgressAsync(int id, int userId, GoalLogProgressDto dto)
    {
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.GoalID == id && g.UserID == userId);

        if (goal == null) return false;

        if (dto.IsIncrement)
        {
            goal.CurrentAmount += dto.Amount;
        }
        else
        {
            goal.CurrentAmount = dto.Amount;
        }

        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            goal.IsCompleted = true;
        }

        goal.LastUpdatedUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.GoalID == id && g.UserID == userId);

        if (goal == null) return false;

        _context.Goals.Remove(goal);
        await _context.SaveChangesAsync();
        return true;
    }
}
