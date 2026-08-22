using FinTrack.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("InvestmentTransactions")]
public class InvestmentTransaction
{
    [Key]
    public int InvestmentTransactionID { get; set; }

    // Parent Investment Relationship
    public int InvestmentID { get; set; }
    public Investment? Investment { get; set; }

    public InvestmentTransactionType TransactionType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(28,8)")]
    public decimal? Quantity { get; set; }

    [Column(TypeName = "decimal(28,8)")]
    public decimal? UnitPrice { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(250)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
