using System.ComponentModel.DataAnnotations;

public class TransactionCreateDto
{
    [Required(ErrorMessage = "Transaction name is required.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than zero.")]
    public decimal TotalAmount { get; set; }

    public string Category { get; set; } = "Uncathegorized";
    public PaymentMethodEnum PaymentMethod { get; set; } = PaymentMethodEnum.Cash;

    public bool IsInstallment { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Installments must be at least 1.")]
    public int TotalInstallments { get; set; } = 1;

    public bool IsRecurrent { get; set; }
    public RecurrenceIntervalEnum RecurrenceInterval { get; set; } = RecurrenceIntervalEnum.None;

    [Required]
    public int UserID { get; set; }
}