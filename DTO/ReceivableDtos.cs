using System.ComponentModel.DataAnnotations;

public class ReceivableItemCreateDto
{
    [Required]
    [MaxLength(100)]
    public string PersonName { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal AmountOwed { get; set; }

    public decimal? AmountPaid { get; set; }

    public bool IsPaid { get; set; } = false;

    public DateTime? PaidDate { get; set; }

    [MaxLength(250)]
    public string? Notes { get; set; }
}

public class ReceivableItemUpdateDto
{
    public int? ReceivableItemID { get; set; }

    [Required]
    [MaxLength(100)]
    public string PersonName { get; set; } = string.Empty;

    [Range(0.01, 999999999.99)]
    public decimal AmountOwed { get; set; }

    public decimal? AmountPaid { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidDate { get; set; }

    [MaxLength(250)]
    public string? Notes { get; set; }
}

public class ReceivableItemReadDto
{
    public int ReceivableItemID { get; set; }
    public int ReceivableID { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public decimal AmountOwed { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ReceivableCreateDto
{
    [Required]
    public int UserID { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal TotalAmount { get; set; }

    public decimal? MyShareAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public DateTime? DueDate { get; set; }

    public List<ReceivableItemCreateDto> Items { get; set; } = new();
}

public class ReceivableUpdateDto
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal TotalAmount { get; set; }

    public decimal? MyShareAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "BRL";

    public DateTime? DueDate { get; set; }

    public bool IsSettled { get; set; }

    public List<ReceivableItemUpdateDto> Items { get; set; } = new();
}

public class ReceivableReadDto
{
    public int ReceivableID { get; set; }
    public int UserID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MyShareAmount { get; set; }
    public decimal TotalOwedByOthers { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalPending { get; set; }
    public decimal ProgressPercentage { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime? DueDate { get; set; }
    public bool IsSettled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
    public List<ReceivableItemReadDto> Items { get; set; } = new();
}

public class DebtorSummaryDto
{
    public string PersonName { get; set; } = string.Empty;
    public decimal TotalOwed { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public int ActiveSharedBillsCount { get; set; }
    public int SettledSharedBillsCount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
}

public class ReceivableSummaryDto
{
    public decimal TotalPendingReceivables { get; set; }
    public decimal TotalCollectedReceivables { get; set; }
    public decimal TotalSharedExpenditures { get; set; }
    public decimal OverallCollectionPercentage { get; set; }
    public int ActiveBillsCount { get; set; }
    public int SettledBillsCount { get; set; }
    public List<ReceivableReadDto> Receivables { get; set; } = new();
    public List<DebtorSummaryDto> Debtors { get; set; } = new();
}
