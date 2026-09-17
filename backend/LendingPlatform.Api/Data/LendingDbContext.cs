using LendingPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LendingPlatform.Api.Data;

public class LendingDbContext(DbContextOptions<LendingDbContext> options) : DbContext(options)
{
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var loan = modelBuilder.Entity<LoanApplication>();

        // SQLite has no native decimal type. EF maps decimal to TEXT by default, which
        // preserves precision but cannot be summed in SQL - aggregation is done in memory
        // (see LoanApplicationService). Fine at this scale; noted in the README as a
        // production consideration.
        loan.Property(a => a.LoanAmount).HasColumnType("decimal(18,2)");
        loan.Property(a => a.AssetValue).HasColumnType("decimal(18,2)");
        loan.Property(a => a.Ltv).HasColumnType("decimal(9,2)");
        loan.Property(a => a.Decision).HasConversion<string>().HasMaxLength(20);
        loan.Property(a => a.Reason).HasMaxLength(300);
    }
}
