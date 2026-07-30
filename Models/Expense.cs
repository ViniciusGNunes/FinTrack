using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;
using FinanceApp.Domain.Enums;

public class Expense
{
    public int ExpenseID { get; set; }

    // Parent Relationship
    public int TransactionID { get; set; }
    public Transaction? Transaction { get; set; }

    /// <summary>
    /// The expected amount due. Allows overrides for variable recurring bills (e.g., AWS $200 in Jan vs $235 in Feb).
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Actual total amount paid so far. Used to handle partial payments.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0.00m;

    /// <summary>
    /// Computed remaining balance for this specific payment line.
    /// </summary>
    [NotMapped]
    public decimal RemainingAmount => Math.Max(0, Amount - PaidAmount);

    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; } // Null until settled/partially paid

    public int CurrentInstallment { get; set; } = 1;
    
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;

    // Direct User Reference for faster queries without joining Parent Transaction
    public int UserID { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}