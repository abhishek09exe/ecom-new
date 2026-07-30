using ecom_new_api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Cart order tables ───────────────────────────────────────────────────────
    public DbSet<CartOrder> CartOrder => Set<CartOrder>();
    public DbSet<CartOrderPartner> CartOrderPartner => Set<CartOrderPartner>();
    public DbSet<CartOrderRoute> CartOrderRoute => Set<CartOrderRoute>();
    public DbSet<CartOrderMessage> CartOrderMessage => Set<CartOrderMessage>();
    public DbSet<CartJson> CartJson => Set<CartJson>();
    public DbSet<CartOrderItem> CartOrderItem => Set<CartOrderItem>();
    public DbSet<CartOrderItemJson> CartOrderItemJson => Set<CartOrderItemJson>();
    public DbSet<CartOrderItemLicense> CartOrderItemLicense => Set<CartOrderItemLicense>();

    // ── Partner / account tables ────────────────────────────────────────────────
    public DbSet<Partner> Partner => Set<Partner>();
    public DbSet<PartnerAccount> PartnerAccount => Set<PartnerAccount>();
    public DbSet<PartnerConfigurationPartner> PartnerConfigurationPartner => Set<PartnerConfigurationPartner>();
    public DbSet<Account> Account => Set<Account>();

    // ── Currency / site tables ──────────────────────────────────────────────────
    public DbSet<Currency> Currency => Set<Currency>();
    public DbSet<CartSiteIdOrderCodePrefix> CartSiteIdOrderCodePrefix => Set<CartSiteIdOrderCodePrefix>();

    // ── Sequence / ID tables ────────────────────────────────────────────────────
    public DbSet<IdsRow> Ids => Set<IdsRow>();

    // ── License tables ──────────────────────────────────────────────────────────
    public DbSet<LicenseKey> LicenseKey => Set<LicenseKey>();
    public DbSet<LicenseCategory> LicenseCategory => Set<LicenseCategory>();
    public DbSet<LicenseKeycodeType> LicenseKeycodeType => Set<LicenseKeycodeType>();
    public DbSet<LicenseAttributeLicenseValue> LicenseAttributeLicenseValue => Set<LicenseAttributeLicenseValue>();

    // ── Product tables ──────────────────────────────────────────────────────────
    public DbSet<Product> Product => Set<Product>();
    public DbSet<ProductFamily> ProductFamily => Set<ProductFamily>();
    public DbSet<ProductType> ProductType => Set<ProductType>();
    public DbSet<ProductLine> ProductLine => Set<ProductLine>();
    public DbSet<ProductLineProduct> ProductLineProduct => Set<ProductLineProduct>();
    public DbSet<ProductLicenseCategory> ProductLicenseCategory => Set<ProductLicenseCategory>();
    public DbSet<ProductYears> ProductYears => Set<ProductYears>();
    public DbSet<ProductSeat> ProductSeat => Set<ProductSeat>();
    public DbSet<ProductPricing> ProductPricing => Set<ProductPricing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // cart_order → cart_order_partner (0..1)
        modelBuilder.Entity<CartOrder>()
            .HasOne(o => o.CartOrderPartner)
            .WithOne(p => p.CartOrder)
            .HasForeignKey<CartOrderPartner>(p => p.CartOrderId);

        // cart_order → cart_json (0..1)
        modelBuilder.Entity<CartOrder>()
            .HasOne(o => o.CartJson)
            .WithOne()
            .HasForeignKey<CartJson>(j => j.CartOrderId);

        // cart_order → cart_order_route (0..1)
        modelBuilder.Entity<CartOrder>()
            .HasOne(o => o.CartOrderRoute)
            .WithOne()
            .HasForeignKey<CartOrderRoute>(r => r.CartOrderId);

        // cart_order → cart_order_message (0..1)
        modelBuilder.Entity<CartOrder>()
            .HasOne(o => o.CartOrderMessage)
            .WithOne()
            .HasForeignKey<CartOrderMessage>(m => m.CartOrderId);

        // cart_order → cart_order_item (1..*)
        modelBuilder.Entity<CartOrder>()
            .HasMany(o => o.Items)
            .WithOne(i => i.CartOrder)
            .HasForeignKey(i => i.CartOrderId);

        // cart_order_partner → partner (many..1)
        modelBuilder.Entity<CartOrderPartner>()
            .HasOne(cp => cp.Partner)
            .WithMany()
            .HasForeignKey(cp => cp.PartnerId);

        // cart_order → currency (many..1)
        modelBuilder.Entity<CartOrder>()
            .HasOne(o => o.Currency)
            .WithMany()
            .HasForeignKey(o => o.CurrencyId);

        // cart_order_item → product (many..1)
        modelBuilder.Entity<CartOrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId);

        // cart_order_item → cart_order_item_json (0..1)
        modelBuilder.Entity<CartOrderItem>()
            .HasOne(i => i.CartOrderItemJson)
            .WithOne()
            .HasForeignKey<CartOrderItemJson>(j => j.CartOrderItemId);

        // cart_order_item → cart_order_item_license (0..1)
        modelBuilder.Entity<CartOrderItem>()
            .HasOne(i => i.CartOrderItemLicense)
            .WithOne()
            .HasForeignKey<CartOrderItemLicense>(l => l.CartOrderItemId);

        // partner_account → account (many..1)
        modelBuilder.Entity<PartnerAccount>()
            .HasOne(pa => pa.Account)
            .WithMany()
            .HasForeignKey(pa => pa.AccountId);

        // product → product_type
        modelBuilder.Entity<Product>()
            .HasOne(p => p.ProductType)
            .WithMany()
            .HasForeignKey(p => p.ProductTypeId);

        // product → product_family
        modelBuilder.Entity<Product>()
            .HasOne(p => p.ProductFamily)
            .WithMany()
            .HasForeignKey(p => p.ProductFamilyId);

        // product → license_keycode_type
        modelBuilder.Entity<Product>()
            .HasOne(p => p.LicenseKeycodeType)
            .WithMany()
            .HasForeignKey(p => p.LicenseKeycodeTypeId);

        // product_line_product
        modelBuilder.Entity<ProductLineProduct>()
            .HasKey(plp => new { plp.ProductId, plp.ProductLineId });

        modelBuilder.Entity<ProductLineProduct>()
            .HasOne(plp => plp.Product)
            .WithMany(p => p.ProductLineProducts)
            .HasForeignKey(plp => plp.ProductId);

        modelBuilder.Entity<ProductLineProduct>()
            .HasOne(plp => plp.ProductLine)
            .WithMany()
            .HasForeignKey(plp => plp.ProductLineId);

        // product_license_category
        modelBuilder.Entity<ProductLicenseCategory>()
            .HasOne(plc => plc.Product)
            .WithMany(p => p.ProductLicenseCategories)
            .HasForeignKey(plc => plc.ProductId);

        modelBuilder.Entity<ProductLicenseCategory>()
            .HasOne(plc => plc.LicenseCategory)
            .WithMany()
            .HasForeignKey(plc => plc.LicenseCategoryId);

        // product_years
        modelBuilder.Entity<ProductYears>()
            .HasOne(py => py.Product)
            .WithMany(p => p.ProductYears)
            .HasForeignKey(py => py.ProductId);

        // product_seat
        modelBuilder.Entity<ProductSeat>()
            .HasOne(ps => ps.Product)
            .WithMany(p => p.ProductSeats)
            .HasForeignKey(ps => ps.ProductId);

        // product_pricing
        modelBuilder.Entity<ProductPricing>()
            .HasOne(pp => pp.Product)
            .WithMany()
            .HasForeignKey(pp => pp.ProductId);
    }
}
