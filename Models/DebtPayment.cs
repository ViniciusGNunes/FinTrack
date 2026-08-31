using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("DebtPayments")]
public class DebtPayment
{
    [Key]
    public int DebtPaymentID { get; set; }

    public int DebtID { get; set; }
    public Debt? Debt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrincipalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? InterestAmount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public int? ExpenseID { get; set; }
    public Expense? Expense { get; set; }

    [MaxLength(250)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
