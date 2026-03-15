using Microsoft.EntityFrameworkCore;
using PotatoWarehouse.Models;

namespace PotatoWarehouse.Data;

public class WarehouseDbContext : DbContext
{
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Variety> Varieties { get; set; }
    public DbSet<Caliber> Calibers { get; set; }
    public DbSet<IncomingPotato> IncomingPotatoes { get; set; }
    public DbSet<OutgoingPotato> OutgoingPotatoes { get; set; }
    public DbSet<AppSettings> AppSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=warehouse.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppSettings>().HasData(new AppSettings { Id = 1 });
    }
}
