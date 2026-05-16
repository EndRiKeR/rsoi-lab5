using Microsoft.EntityFrameworkCore;
using StatisticService.Database.Models;

namespace StatisticService.Database;

public class StatisticContext : DbContext
{
    public StatisticContext(DbContextOptions<StatisticContext> options) : base(options) { }
    
    public DbSet<StatisticsEvent> Events { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}