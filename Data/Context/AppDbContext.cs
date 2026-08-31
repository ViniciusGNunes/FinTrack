using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>,int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Expense> Expenses {get;set;}
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Investment> Investments { get; set; }
    public DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }
    public DbSet<MarketPriceHistory> MarketPriceHistories { get; set; }
    public DbSet<Debt> Debts { get; set; }
    public DbSet<DebtPayment> DebtPayments { get; set; }
    public DbSet<Receivable> Receivables { get; set; }
    public DbSet<ReceivableItem> ReceivableItems { get; set; }
    public DbSet<Goal> Goals { get; set; }
}