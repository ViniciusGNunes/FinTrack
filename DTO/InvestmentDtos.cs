using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;

public class InvestmentCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Ticker { get; set; }

    public InvestmentType InvestmentType { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    [Range(0.01, 999999999.99)]
    public decimal TotalInvested { get; set; }

    // Variable Income
    public decimal? Quantity { get; set; }
    public decimal? PurchasePricePerUnit { get; set; }

    // Fixed Income
    public FixedRateType? RateType { get; set; }
    public decimal? AnnualRate { get; set; }
    public bool IsTaxExempt { get; set; } = false;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? MaturityDate { get; set; }

    public int UserID { get; set; }
}

public class InvestmentUpdateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Ticker { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public decimal? Quantity { get; set; }
    public decimal? PurchasePricePerUnit { get; set; }
    public decimal? CurrentPricePerUnit { get; set; }

    public FixedRateType? RateType { get; set; }
    public decimal? AnnualRate { get; set; }
    public bool IsTaxExempt { get; set; }

    public DateTime? MaturityDate { get; set; }
}

public class InvestmentReadDto
{
    public int InvestmentID { get; set; }
    public int UserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public InvestmentType InvestmentType { get; set; }
    public string Currency { get; set; } = "BRL";
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLossAmount { get; set; }
    public decimal ProfitLossPercentage { get; set; }

    // Variable Income
    public decimal? Quantity { get; set; }
    public decimal? PurchasePricePerUnit { get; set; }
    public decimal? CurrentPricePerUnit { get; set; }

    // Fixed Income
    public FixedRateType? RateType { get; set; }
    public decimal? AnnualRate { get; set; }
    public bool IsTaxExempt { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public bool IsLiquidated { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    public List<InvestmentTransactionReadDto> Transactions { get; set; } = new();
}

public class InvestmentTransactionCreateDto
{
    public InvestmentTransactionType TransactionType { get; set; }

    [Range(0.00, 999999999.99)]
    public decimal Amount { get; set; }

    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(250)]
    public string? Notes { get; set; }
}

public class InvestmentTransactionReadDto
{
    public int InvestmentTransactionID { get; set; }
    public int InvestmentID { get; set; }
    public InvestmentTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
}

public class InvestmentGrowthPointDto
{
    public DateTime Date { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLossAmount { get; set; }
    public decimal ProfitLossPercentage { get; set; }
}

public class PortfolioSummaryDto
{
    public decimal TotalInvested { get; set; }
    public decimal TotalCurrentValue { get; set; }
    public decimal TotalProfitLossAmount { get; set; }
    public decimal TotalProfitLossPercentage { get; set; }
    public List<InvestmentReadDto> Investments { get; set; } = new();
}
