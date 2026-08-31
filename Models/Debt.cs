using FinTrack.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Debts")]
public class Debt
{
    [Key]
    public int DebtID { get; set; }

    // User Relationship
    public int UserID { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Issuer { get; set; } = string.Empty;

    public DebtType DebtType { get; set; } = DebtType.Personal;

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    [Column(TypeName = "decimal(18,2)")]
    public decimal OriginalPrincipal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingBalance { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal InterestRate { get; set; }

    public DebtRateType RateType { get; set; } = DebtRateType.FixedAnnual;

    public RecurrenceInterval PaymentFrequency { get; set; } = RecurrenceInterval.Monthly;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? InstallmentAmount { get; set; }

    public int? TotalInstallments { get; set; }

    public int PaidInstallments { get; set; } = 0;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public int? DueDay { get; set; }

    public DateTime? MaturityDate { get; set; }

    public bool IsPaidOff { get; set; } = false;

    public bool AutoGenerateExpenses { get; set; } = true;

    public int? TransactionID { get; set; }
    public Transaction? Transaction { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation Property to Payments
    public ICollection<DebtPayment> Payments { get; set; } = new List<DebtPayment>();

    // --- Computed Domain Helpers ---
    [NotMapped]
    public decimal TotalPaidAmount => Math.Max(0, OriginalPrincipal - RemainingBalance);

    [NotMapped]
    public decimal ProgressPercentage => OriginalPrincipal > 0
        ? Math.Min(100, Math.Round(((OriginalPrincipal - RemainingBalance) / OriginalPrincipal) * 100, 2))
        : 100m;
}
