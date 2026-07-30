using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

public class PayExpenseDto
{
    /// <summary>
    /// Optional date when payment occurred. Defaults to UTC NOW if null.
    /// </summary>
    public DateTime? PaidDate { get; set; }
}

public class PartialPayExpenseDto
{
    [Range(0.01, 999999999.99)]
    public decimal PaymentAmount { get; set; }

    public DateTime? PaidDate { get; set; }
}

public class UpdateExpenseAmountDto
{
    [Range(0.01, 999999999.99)]
    public decimal NewAmount { get; set; }
}

public class DetailedExpenseReadDto
{
    public int ExpenseID { get; set; }
    public int TransactionID { get; set; }
    public string TransactionName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => Math.Max(0, Amount - PaidAmount);

    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }

    public int CurrentInstallment { get; set; }
    public int TotalInstallments { get; set; }
    public bool IsInstallment { get; set; }

    public ExpenseStatus Status { get; set; }
    public int UserID { get; set; }
}