using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Transaction
{
    public int TransactionID { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public string Category { get; set; } = "Uncathegorized";
    public PaymentMethodEnum PaymentMethod { get; set; }

    public bool IsInstallment { get; set; }
    public int TotalInstallments { get; set; }
    public bool IsRecurrent { get; set; }
    public RecurrenceIntervalEnum RecurrenceInterval { get; set; }
    public int UserID { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}