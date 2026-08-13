namespace FinTrack.Domain.Enums;

public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    Pix = 5
}

public enum RecurrenceInterval
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4
}

public enum TransactionType
{
    Expense = 1,  // Money going out
    Income = 2    // Salary, side hustles, investment returns
}

public enum TransactionStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}

public enum ExpenseStatus
{
    Pending = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5,
    PartiallyRefunded = 6,
    Refunded = 7
}

public enum TimeCategory{
  Last = 1,
  Current = 2,
  Next = 3
}

public enum TimePeriod{
  Day = 1,
  Week = 2,
  TwoWeeks = 3,
  Month = 4,
  Year = 5
}
