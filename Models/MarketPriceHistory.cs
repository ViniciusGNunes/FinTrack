using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("MarketPriceHistories")]
[Index(nameof(Symbol), nameof(Date), IsUnique = true)]
public class MarketPriceHistory
{
    [Key]
    public int MarketPriceHistoryID { get; set; }

    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty; // e.g. "PETR4.SA", "AAPL", "CDI", "SELIC", "USDBRL"

    public DateOnly Date { get; set; }

    [Column(TypeName = "decimal(28,8)")]
    public decimal ClosePrice { get; set; } // Daily stock close price, daily rate factor, crypto price, or FX rate

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
