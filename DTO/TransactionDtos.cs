using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

public class TransactionCreateDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal TotalAmount { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;
    public int CategoryID { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public bool IsInstallment { get; set; }
    
    [Range(1, 360)]
    public int TotalInstallments { get; set; } = 1;

    public bool IsRecurrent { get; set; }
    public RecurrenceInterval RecurrenceInterval { get; set; } = RecurrenceInterval.None;
    public DateTime FirstDueDate { get; set; } = DateTime.UtcNow;

    public int UserID { get; set; }
}

public class TransactionUpdateDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public int CategoryID { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}

public class TransactionReadDto
{
    public int TransactionID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public bool IsInstallment { get; set; }
    public int TotalInstallments { get; set; }
    public bool IsRecurrent { get; set; }
    public RecurrenceInterval RecurrenceInterval { get; set; }
    public int? RecurrenceTargetDay { get; set; }
    public int UserID { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    
    public List<ExpenseReadDto> Expenses { get; set; } = new();
}

public class ExpenseReadDto
{
    public int ExpenseID { get; set; }
    public int TransactionID { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => Math.Max(0, Amount - PaidAmount);
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public int CurrentInstallment { get; set; }
    public ExpenseStatus Status { get; set; }
}