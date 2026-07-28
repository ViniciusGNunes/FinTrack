public class TransactionReadDto
{
    public int TransactionID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Category { get; set; } = "Uncathegorized";
    public PaymentMethodEnum PaymentMethod { get; set; }
    public bool IsInstallment { get; set; }
    public int TotalInstallments { get; set; }
    public bool IsRecurrent { get; set; }
    public RecurrenceIntervalEnum RecurrenceInterval { get; set; }
    public int UserID { get; set; }
}