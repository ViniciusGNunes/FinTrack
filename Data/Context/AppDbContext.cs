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

}