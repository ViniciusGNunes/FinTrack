using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;

public class GoalCreateDto
{
    [Required]
    public int UserID { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public GoalCategory Category { get; set; }

    public GoalFrequency Frequency { get; set; } = GoalFrequency.Monthly;

    [Range(0.01, 999999999.99)]
    public decimal TargetAmount { get; set; }

    public decimal? InitialAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public int? LinkedDebtID { get; set; }
    public int? LinkedCategoryID { get; set; }

    public DateTime? TargetDate { get; set; }

    public bool AutoTrack { get; set; } = true;
}

public class GoalUpdateDto
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public GoalCategory Category { get; set; }

    public GoalFrequency Frequency { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal TargetAmount { get; set; }

    public decimal? CurrentAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public int? LinkedDebtID { get; set; }
    public int? LinkedCategoryID { get; set; }

    public DateTime? TargetDate { get; set; }

    public bool AutoTrack { get; set; }

    public bool IsCompleted { get; set; }
}

public class GoalLogProgressDto
{
    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    public bool IsIncrement { get; set; } = true;
}

public class GoalReadDto
{
    public int GoalID { get; set; }
    public int UserID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalCategory Category { get; set; }
    public GoalFrequency Frequency { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Currency { get; set; } = "BRL";
    public int? LinkedDebtID { get; set; }
    public string? LinkedDebtName { get; set; }
    public int? LinkedCategoryID { get; set; }
    public string? LinkedCategoryName { get; set; }
    public DateTime? TargetDate { get; set; }
    public bool AutoTrack { get; set; }
    public bool IsCompleted { get; set; }
    public string PacingStatus { get; set; } = "OnTrack"; // OnTrack, BehindPace, OverBudget, Achieved
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}

public class GoalSummaryDto
{
    public int TotalGoalsCount { get; set; }
    public int ActiveGoalsCount { get; set; }
    public int CompletedGoalsCount { get; set; }
    public decimal MonthlyInvestmentTarget { get; set; }
    public decimal MonthlyInvestmentActual { get; set; }
    public decimal MonthlyDebtReductionTarget { get; set; }
    public decimal MonthlyDebtReductionActual { get; set; }
    public decimal OverallProgressPercentage { get; set; }
    public List<GoalReadDto> Goals { get; set; } = new();
}
