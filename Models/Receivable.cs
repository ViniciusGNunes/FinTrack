using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Receivables")]
public class Receivable
{
    [Key]
    public int ReceivableID { get; set; }

    public int UserID { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MyShareAmount { get; set; } = 0.00m;

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public DateTime? DueDate { get; set; }

    public bool IsSettled { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ReceivableItem> Items { get; set; } = new List<ReceivableItem>();

    // --- Computed Domain Helpers ---
    [NotMapped]
    public decimal TotalOwedByOthers => Items.Sum(i => i.AmountOwed);

    [NotMapped]
    public decimal TotalCollected => Items.Where(i => i.IsPaid).Sum(i => i.AmountPaid > 0 ? i.AmountPaid : i.AmountOwed);

    [NotMapped]
    public decimal TotalPending => Math.Max(0, TotalOwedByOthers - TotalCollected);

    [NotMapped]
    public decimal ProgressPercentage => TotalOwedByOthers > 0
        ? Math.Min(100, Math.Round((TotalCollected / TotalOwedByOthers) * 100, 2))
        : 100m;
}
