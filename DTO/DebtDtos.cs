using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;

public class DebtCreateDto
{
    [Required]
    public int UserID { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Issuer { get; set; } = string.Empty;

    public DebtType DebtType { get; set; } = DebtType.Personal;

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    [Range(0.01, 999999999.99)]
    public decimal OriginalPrincipal { get; set; }

    public decimal? RemainingBalance { get; set; }

    [Range(0.00, 999.99)]
    public decimal InterestRate { get; set; }

    public DebtRateType RateType { get; set; } = DebtRateType.FixedAnnual;

    public RecurrenceInterval PaymentFrequency { get; set; } = RecurrenceInterval.Monthly;

    public decimal? InstallmentAmount { get; set; }

    public int? TotalInstallments { get; set; }

    public int PaidInstallments { get; set; } = 0;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public int? DueDay { get; set; }

    public DateTime? MaturityDate { get; set; }

    public bool AutoGenerateExpenses { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class DebtUpdateDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Issuer { get; set; } = string.Empty;

    public DebtType DebtType { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public decimal? RemainingBalance { get; set; }

    public decimal InterestRate { get; set; }

    public DebtRateType RateType { get; set; }

    public RecurrenceInterval PaymentFrequency { get; set; }

    public decimal? InstallmentAmount { get; set; }

    public int? TotalInstallments { get; set; }

    public int? DueDay { get; set; }

    public DateTime? MaturityDate { get; set; }

    public bool IsPaidOff { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class DebtReadDto
{
    public int DebtID { get; set; }
    public int UserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DebtType DebtType { get; set; }
    public string Currency { get; set; } = "BRL";
    public decimal OriginalPrincipal { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public decimal InterestRate { get; set; }
    public DebtRateType RateType { get; set; }
    public RecurrenceInterval PaymentFrequency { get; set; }
    public decimal? InstallmentAmount { get; set; }
    public int? TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public DateTime StartDate { get; set; }
    public int? DueDay { get; set; }
    public DateTime? MaturityDate { get; set; }
    public bool IsPaidOff { get; set; }
    public bool AutoGenerateExpenses { get; set; }
    public int? TransactionID { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
    public List<DebtPaymentReadDto> Payments { get; set; } = new();
}

public class DebtPaymentCreateDto
{
    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    public decimal? PrincipalAmount { get; set; }
    public decimal? InterestAmount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public int? ExpenseID { get; set; }

    [MaxLength(250)]
    public string? Notes { get; set; }
}

public class DebtPaymentReadDto
{
    public int DebtPaymentID { get; set; }
    public int DebtID { get; set; }
    public decimal Amount { get; set; }
    public decimal? PrincipalAmount { get; set; }
    public decimal? InterestAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int? ExpenseID { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DebtScheduleItemDto
{
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal ScheduledPayment { get; set; }
    public decimal PrincipalPortion { get; set; }
    public decimal InterestPortion { get; set; }
    public decimal RemainingBalanceAfter { get; set; }
    public bool IsPaid { get; set; }
}

public class DebtSummaryDto
{
    public decimal TotalOriginalPrincipal { get; set; }
    public decimal TotalRemainingBalance { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal OverallProgressPercentage { get; set; }
    public decimal TotalMonthlyObligation { get; set; }
    public decimal WeightedAverageInterestRate { get; set; }
    public int ActiveDebtsCount { get; set; }
    public int PaidOffDebtsCount { get; set; }
    public List<DebtReadDto> Debts { get; set; } = new();
}
