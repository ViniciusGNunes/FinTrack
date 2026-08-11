using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinTrack.Domain.Enums;

public class Transaction
{
    [Key]
    public int TransactionID { get; set; }
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundedAmount { get; set; } = 0.00m;
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public PaymentMethod PaymentMethod { get; set; }
    public int CategoryID { get; set; }
    public Category? Category { get; set; }
    public bool IsInstallment { get; set; }
    public int TotalInstallments { get; set; } = 1;
    public bool IsRecurrent { get; set; }
    public RecurrenceInterval RecurrenceInterval { get; set; } = RecurrenceInterval.None;
    public int? RecurrenceTargetDay { get; set; }
    public DateTime? CancellationDate { get; set; }
    public int UserID { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}