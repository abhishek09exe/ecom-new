using Microsoft.EntityFrameworkCore;
using ecom_new_api.Data.Entities;

namespace ecom_new_api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<CartOrder> CartOrders => Set<CartOrder>();
    public DbSet<CartOrderPartner> CartOrderPartners => Set<CartOrderPartner>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CartJson> CartJsons => Set<CartJson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CartOrder -> Currency (Many : One)
        modelBuilder.Entity<CartOrder>()
            .HasOne(c => c.Currency)
            .WithMany(cu => cu.CartOrders)
            .HasForeignKey(c => c.CurrencyId);

        // CartOrder -> CartJson (One : One)
        modelBuilder.Entity<CartOrder>()
            .HasOne(c => c.CartJson)
            .WithOne(j => j.CartOrder)
            .HasForeignKey<CartJson>(j => j.CartOrderId);

        // CartOrder -> CartOrderPartner (One : Many)
        modelBuilder.Entity<CartOrder>()
            .HasMany(c => c.CartOrderPartners)
            .WithOne(cp => cp.CartOrder)
            .HasForeignKey(cp => cp.CartOrderId);

        // CartOrderPartner -> Partner (Many : One)
        modelBuilder.Entity<CartOrderPartner>()
            .HasOne(cp => cp.Partner)
            .WithMany(p => p.CartOrderPartners)
            .HasForeignKey(cp => cp.PartnerId);
    }
}