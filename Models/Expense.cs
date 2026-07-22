using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

public class Expense
{
    public int ExpenseID { get; set; }
    public int TransactionID { get; set; }
    public Transaction? Transaction { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public int CurrentInstallment { get; set; }
    public bool IsPaid { get; set; }
    public int UserID { get; set; }
}