using FinTrack.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

[Table("Expenses")]
public class Expense
{
    [Key]
    public int ExpenseID { get; set; }

    // Parent Relationship
    public int TransactionID { get; set; }
    public Transaction? Transaction { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundedAmount { get; set; } = 0.00m;

    public DateTime? RefundDate { get; set; }

    [MaxLength(250)]
    public string? RefundReason { get; set; }

    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }

    public int CurrentInstallment { get; set; } = 1;

    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;

    // Direct User Reference
    public int UserID { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // --- Computed Domain Helpers ---
    [NotMapped]
    public decimal RemainingAmount => Math.Max(0, Amount - PaidAmount);

    [NotMapped]
    public decimal NetAmount => Math.Max(0, Amount - RefundedAmount);
}