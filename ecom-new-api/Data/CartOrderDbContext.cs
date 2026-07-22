using Microsoft.EntityFrameworkCore;
using ecom_new_api.Data.Entities;

namespace ecom_new_api.Data;

/// <summary>
/// Entity Framework Core DbContext for the eCommerce cart application
/// Connects to SQL Server database [ecommerce_VH14]
/// 
/// Usage:
///   services.AddDbContext<CartOrderDbContext>(options =>
///       options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
///   );
/// </summary>
public class CartOrderDbContext : DbContext
{
    public CartOrderDbContext(DbContextOptions<CartOrderDbContext> options)
        : base(options)
    {
    }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Core domain entities
    // ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shopping cart headers
    /// Table: [cart_order]
    /// </summary>
    public DbSet<CartOrderEntity> CartOrders { get; set; }

    /// <summary>
    /// Line items within a cart order
    /// Table: [cart_order_item]
    /// </summary>
    public DbSet<CartOrderItemEntity> CartOrderItems { get; set; }

    /// <summary>
    /// Partner/vendor reference data
    /// Table: [partner]
    /// </summary>
    public DbSet<PartnerEntity> Partners { get; set; }

    /// <summary>
    /// Currency reference data (USD, EUR, etc.)
    /// Table: [currency]
    /// </summary>
    public DbSet<CurrencyEntity> Currencies { get; set; }

    /// <summary>
    /// Product catalog
    /// Table: [product]
    /// </summary>
    public DbSet<ProductEntity> Products { get; set; }

    /// <summary>
    /// License categories (SOHO, SMB, ENT, OTSF, etc.)
    /// Table: [license_category]
    /// </summary>
    public DbSet<LicenseCategoryEntity> LicenseCategories { get; set; }

    /// <summary>
    /// License records (keycodes)
    /// Table: [license]
    /// </summary>
    public DbSet<LicenseEntity> Licenses { get; set; }

    /// <summary>
    /// License attribute values (billing models, etc.)
    /// Table: [license_attribute_license_value]
    /// </summary>
    public DbSet<LicenseAttributeEntity> LicenseAttributes { get; set; }

    /// <summary>
    /// License messages (for monthly process dates, etc.)
    /// Table: [license_message]
    /// </summary>
    public DbSet<LicenseMessageEntity> LicenseMessages { get; set; }

    /// <summary>
    /// Product line assignments by license category
    /// Table: [license_category_product_line]
    /// </summary>
    public DbSet<LicenseCategoryProductLineEntity> LicenseCategoryProductLines { get; set; }

    /// <summary>
    /// Locale to language/location mappings
    /// Table: [locale_language_location] or function [fn_locale_to_lang_loc]
    /// </summary>
    public DbSet<LocaleLanguageLocationEntity> LocaleLanguageLocations { get; set; }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Junction and extension tables
    // ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Links cart orders to partners
    /// Table: [cart_order_partner]
    /// </summary>
    public DbSet<CartOrderPartnerEntity> CartOrderPartners { get; set; }

    /// <summary>
    /// Extension JSON data for cart orders
    /// Table: [cart_json]
    /// </summary>
    public DbSet<CartOrderJsonEntity> CartOrderJsons { get; set; }

    /// <summary>
    /// Extension JSON data for cart order items
    /// Table: [cart_order_item_json]
    /// </summary>
    public DbSet<CartOrderItemJsonEntity> CartOrderItemJsons { get; set; }

    /// <summary>
    /// Logging table for JSON payloads during item insertion
    /// Table: [cart_order_item_json_log]
    /// </summary>
    public DbSet<CartOrderItemJsonLogEntity> CartOrderItemJsonLogs { get; set; }

    /// <summary>
    /// Links cart order items to license keycodes
    /// Table: [cart_order_item_license]
    /// </summary>
    public DbSet<CartOrderItemLicenseEntity> CartOrderItemLicenses { get; set; }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Fluent API Configuration
    // ──────────────────────────────────────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrder → Currency (Many-to-One)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderEntity>()
            .HasOne(c => c.Currency)
            .WithMany(c => c.CartOrders)
            .HasForeignKey(c => c.CurrencyId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_cart_order_currency_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrder → CartOrderItems (One-to-Many)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderEntity>()
            .HasMany(c => c.CartOrderItems)
            .WithOne(i => i.CartOrder)
            .HasForeignKey(i => i.CartOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_cart_order_item_cart_order_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrder → CartOrderPartners (One-to-Many)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderEntity>()
            .HasMany(c => c.CartOrderPartners)
            .WithOne(p => p.CartOrder)
            .HasForeignKey(p => p.CartOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_cart_order_partner_cart_order_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrder → CartOrderJson (One-to-Zero/One)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderEntity>()
            .HasOne(c => c.CartJson)
            .WithOne(j => j.CartOrder)
            .HasForeignKey<CartOrderJsonEntity>(j => j.CartOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_cart_json_cart_order_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrderItem → Product (Many-to-One)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderItemEntity>()
            .HasOne(i => i.Product)
            .WithMany(p => p.CartOrderItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_cart_order_item_product_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrderItem → CartOrderItemJson (One-to-Zero/One)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderItemEntity>()
            .HasOne(i => i.CartOrderItemJson)
            .WithOne(j => j.CartOrderItem)
            .HasForeignKey<CartOrderItemJsonEntity>(j => j.CartOrderItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_cart_order_item_json_cart_order_item_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrderItem → CartOrderItemLicenses (One-to-Many)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderItemEntity>()
            .HasMany(i => i.CartOrderItemLicenses)
            .WithOne(l => l.CartOrderItem)
            .HasForeignKey(l => l.CartOrderItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_cart_order_item_license_cart_order_item_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // CartOrderPartner → Partner (Many-to-One)
        // ──────────────────────────────────────────────────────────────────────────────────────
        modelBuilder
            .Entity<CartOrderPartnerEntity>()
            .HasOne(p => p.Partner)
            .WithMany(pr => pr.CartOrderPartners)
            .HasForeignKey(p => p.PartnerId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_cart_order_partner_partner_id");

        // ──────────────────────────────────────────────────────────────────────────────────────
        // Index configuration for performance
        // ──────────────────────────────────────────────────────────────────────────────────────

        // CartOrder indexes
        modelBuilder
            .Entity<CartOrderEntity>()
            .HasIndex(c => c.VendorOrderCode)
            .IsUnique()
            .HasDatabaseName("IX_cart_order_vendor_order_code");

        modelBuilder
            .Entity<CartOrderEntity>()
            .HasIndex(c => c.CurrencyId)
            .HasDatabaseName("IX_cart_order_currency_id");

        // CartOrderItem indexes
        modelBuilder
            .Entity<CartOrderItemEntity>()
            .HasIndex(i => i.CartOrderId)
            .HasDatabaseName("IX_cart_order_item_cart_order_id");

        modelBuilder
            .Entity<CartOrderItemEntity>()
            .HasIndex(i => i.ProductId)
            .HasDatabaseName("IX_cart_order_item_product_id");

        // CartOrderPartner indexes
        modelBuilder
            .Entity<CartOrderPartnerEntity>()
            .HasIndex(p => new { p.CartOrderId, p.PartnerId })
            .HasDatabaseName("IX_cart_order_partner_cart_order_partner");
    }
}
