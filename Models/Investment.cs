using FinTrack.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Investments")]
public class Investment
{
    [Key]
    public int InvestmentID { get; set; }

    // User Relationship
    public int UserID { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Ticker { get; set; }

    public InvestmentType InvestmentType { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalInvested { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentValue { get; set; }

    // Variable Income & Crypto Fields
    [Column(TypeName = "decimal(28,8)")]
    public decimal? Quantity { get; set; }

    [Column(TypeName = "decimal(28,8)")]
    public decimal? PurchasePricePerUnit { get; set; }

    [Column(TypeName = "decimal(28,8)")]
    public decimal? CurrentPricePerUnit { get; set; }

    // Fixed Income Fields
    public FixedRateType? RateType { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal? AnnualRate { get; set; }

    public bool IsTaxExempt { get; set; } = false;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? MaturityDate { get; set; }

    public bool IsLiquidated { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation Property to Transactions
    public ICollection<InvestmentTransaction> Transactions { get; set; } = new List<InvestmentTransaction>();

    // --- Computed Domain Helpers ---
    [NotMapped]
    public decimal ProfitLossAmount => CurrentValue - TotalInvested;

    [NotMapped]
    public decimal ProfitLossPercentage => TotalInvested > 0 
        ? Math.Round(((CurrentValue - TotalInvested) / TotalInvested) * 100, 2) 
        : 0.00m;
}
