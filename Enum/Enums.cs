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

public enum InvestmentType
{
    FixedIncome = 0,
    VariableIncome = 1,
    Crypto = 2
}

public enum FixedRateType
{
    Prefixado = 0,
    Selic_CDI = 1,
    IPCA_Plus = 2
}

public enum InvestmentTransactionType
{
    Buy = 0,          // Deposit / Buy shares
    Sell = 1,         // Withdrawal / Sell shares
    Liquidate = 2,    // Position fully closed
    Dividend = 3,     // Yield / Dividend payout
    StockSplit = 4    // Stock split / consolidation
}

public enum DebtType
{
    Personal = 0,
    Bank = 1,
    Student = 2,
    Financing_Mortgage = 3,
    CreditCard = 4,
    Other = 5
}

public enum DebtRateType
{
    FixedAnnual = 0,
    FixedMonthly = 1,
    CDI_Linked = 2,
    IPCA_Linked = 3
}

public enum GoalCategory
{
    MonthlyInvestment = 0,
    MonthlyDebtReduction = 1,
    ExpenseCap = 2,
    TargetSavings = 3,
    PortfolioMilestone = 4
}

public enum GoalFrequency
{
    Monthly = 0,
    OneTimeTarget = 1,
    Yearly = 2
}


