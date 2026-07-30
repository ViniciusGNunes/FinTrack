using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinanceApp.Domain.Enums;

public class Transaction
{
    public int TransactionID { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;

    // Foreign Key to Category
    public int CategoryID { get; set; }
    public Category? Category { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    // --- Installment Logic ---
    public bool IsInstallment { get; set; }
    public int TotalInstallments { get; set; } = 1; // Default to 1 for single purchases

    // --- Recurrence Logic ---
    public bool IsRecurrent { get; set; }
    public RecurrenceInterval RecurrenceInterval { get; set; } = RecurrenceInterval.None;
    
    /// <summary>
    /// Remembers the original anchor day (e.g., 31st).
    /// Used to prevent date-drift when jumping between shorter/longer months (e.g., Jan 31 -> Feb 28 -> Mar 31).
    /// </summary>
    public int? RecurrenceTargetDay { get; set; }

    // --- User & Audit Trail ---
    public int UserID { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // --- Navigation Properties ---
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}