using FinTrack.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Goals")]
public class Goal
{
    [Key]
    public int GoalID { get; set; }

    public int UserID { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public GoalCategory Category { get; set; } = GoalCategory.MonthlyInvestment;

    public GoalFrequency Frequency { get; set; } = GoalFrequency.Monthly;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentAmount { get; set; } = 0.00m;

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public int? LinkedDebtID { get; set; }
    public Debt? LinkedDebt { get; set; }

    public int? LinkedCategoryID { get; set; }
    public Category? LinkedCategory { get; set; }

    public DateTime? TargetDate { get; set; }

    public bool AutoTrack { get; set; } = true;

    public bool IsCompleted { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    // --- Computed Domain Helpers ---
    [NotMapped]
    public decimal ProgressPercentage => TargetAmount > 0
        ? Math.Min(100, Math.Round((CurrentAmount / TargetAmount) * 100, 2))
        : 100m;

    [NotMapped]
    public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);
}
