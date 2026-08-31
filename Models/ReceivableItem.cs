using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ReceivableItems")]
public class ReceivableItem
{
    [Key]
    public int ReceivableItemID { get; set; }

    public int ReceivableID { get; set; }
    public Receivable? Receivable { get; set; }

    [Required]
    [MaxLength(100)]
    public string PersonName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountOwed { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; } = 0.00m;

    public bool IsPaid { get; set; } = false;

    public DateTime? PaidDate { get; set; }

    [MaxLength(250)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
