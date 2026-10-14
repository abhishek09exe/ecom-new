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
    public DbSet<CurrencyLanguageLocation> CurrencyLanguageLocations => Set<CurrencyLanguageLocation>();
    public DbSet<CartSiteIdOrderCodePrefix> CartSiteIdOrderCodePrefix => Set<CartSiteIdOrderCodePrefix>();

    // ── Sequence / ID tables ────────────────────────────────────────────────────
    public DbSet<IdsRow> Ids => Set<IdsRow>();

    // ── License tables ──────────────────────────────────────────────────────────
    public DbSet<LicenseKey> LicenseKey => Set<LicenseKey>();
    public DbSet<LicenseFeatureLicense> LicenseFeatureLicense => Set<LicenseFeatureLicense>();
    public DbSet<LicenseCategory> LicenseCategory => Set<LicenseCategory>();
    public DbSet<LicenseCategoryCapability> LicenseCategoryCapability => Set<LicenseCategoryCapability>();
    public DbSet<ProductLineLicenseDistributionMethod> ProductLineLicenseDistributionMethod => Set<ProductLineLicenseDistributionMethod>();
    public DbSet<LicenseModuleCategory> LicenseModuleCategory => Set<LicenseModuleCategory>();
    public DbSet<LicenseModule> LicenseModule => Set<LicenseModule>();
    public DbSet<LicenseModuleType> LicenseModuleType => Set<LicenseModuleType>();
    public DbSet<LicenseModuleLicense> LicenseModuleLicense => Set<LicenseModuleLicense>();
    public DbSet<LicenseKeycodeType> LicenseKeycodeType => Set<LicenseKeycodeType>();
    public DbSet<LicenseAttributeLicenseValue> LicenseAttributeLicenseValue => Set<LicenseAttributeLicenseValue>();
    public DbSet<License> License => Set<License>();
    public DbSet<LicenseStatus> LicenseStatus => Set<LicenseStatus>();
    public DbSet<LicenseType> LicenseType => Set<LicenseType>();
    public DbSet<LicenseCategoryLicense> LicenseCategoryLicense => Set<LicenseCategoryLicense>();
    public DbSet<LicenseSeat> LicenseSeat => Set<LicenseSeat>();
    public DbSet<LicenseActiveSeats> LicenseActiveSeats => Set<LicenseActiveSeats>();
    public DbSet<LicenseStorage> LicenseStorage => Set<LicenseStorage>();
    public DbSet<LicenseParent> LicenseParent => Set<LicenseParent>();
    public DbSet<LicenseAttribute> LicenseAttribute => Set<LicenseAttribute>();
    public DbSet<LicenseAttributeLicense> LicenseAttributeLicense => Set<LicenseAttributeLicense>();
    public DbSet<LicenseDistributionMethod> LicenseDistributionMethod => Set<LicenseDistributionMethod>();
    public DbSet<LicenseNextBillDate> LicenseNextBillDate => Set<LicenseNextBillDate>();
    public DbSet<LicenseHistory> LicenseHistory => Set<LicenseHistory>();
    public DbSet<LicenseDistributionMethodChannel> LicenseDistributionMethodChannel => Set<LicenseDistributionMethodChannel>();
    public DbSet<Channel> Channel => Set<Channel>();
    public DbSet<Customer> Customer => Set<Customer>();
    public DbSet<OrderItem> OrderItem => Set<OrderItem>();
    public DbSet<OrderItemLicense> OrderItemLicense => Set<OrderItemLicense>();
    public DbSet<LicenseCapability> LicenseCapability => Set<LicenseCapability>();
    public DbSet<CapabilityType> CapabilityType => Set<CapabilityType>();
    public DbSet<LicenseProfileFunctionRow> LicenseProfileFunctionRows => Set<LicenseProfileFunctionRow>();
    public DbSet<LicenseByIdProcedureRow> LicenseByIdProcedureRows => Set<LicenseByIdProcedureRow>();

    // ── Skyrise audit tables ─────────────────────────────────────────────────────
    public DbSet<SkyriseFailure> SkyriseFailure => Set<SkyriseFailure>();
    public DbSet<LicenseServiceType> LicenseServiceType => Set<LicenseServiceType>();
    public DbSet<LicenseServiceStatus> LicenseServiceStatus => Set<LicenseServiceStatus>();
    public DbSet<LicenseService> LicenseService => Set<LicenseService>();
    public DbSet<LicenseServiceJson> LicenseServiceJson => Set<LicenseServiceJson>();

    // ── Product tables ──────────────────────────────────────────────────────────
    public DbSet<Product> Product => Set<Product>();
    public DbSet<ProductFamily> ProductFamily => Set<ProductFamily>();
    public DbSet<ProductType> ProductType => Set<ProductType>();
    public DbSet<ProductLine> ProductLine => Set<ProductLine>();
    public DbSet<ProductLineProduct> ProductLineProduct => Set<ProductLineProduct>();
    public DbSet<LicenseCategoryProductLine> LicenseCategoryProductLine => Set<LicenseCategoryProductLine>();
    public DbSet<ProductCapability> ProductCapability => Set<ProductCapability>();
    public DbSet<LuCountry> LuCountry => Set<LuCountry>();
    public DbSet<ProductLicenseCategory> ProductLicenseCategory => Set<ProductLicenseCategory>();
    public DbSet<ProductLicenseCategorySeat> ProductLicenseCategorySeat => Set<ProductLicenseCategorySeat>();
    public DbSet<ProductLicenseCategoryYears> ProductLicenseCategoryYears => Set<ProductLicenseCategoryYears>();
    public DbSet<ProductLicenseCategoryUpgrade> ProductLicenseCategoryUpgrade => Set<ProductLicenseCategoryUpgrade>();
    public DbSet<ProductYears> ProductYears => Set<ProductYears>();
    public DbSet<ProductSeat> ProductSeat => Set<ProductSeat>();
    public DbSet<ProductPricing> ProductPricing => Set<ProductPricing>();
    public DbSet<ItemHierarchy> ItemHierarchy => Set<ItemHierarchy>();
    public DbSet<CartDiscount> CartDiscount => Set<CartDiscount>();
    public DbSet<CartDiscountItem> CartDiscountItem => Set<CartDiscountItem>();
    public DbSet<MessageCampaign> MessageCampaign => Set<MessageCampaign>();
    public DbSet<MessageCampaignPlatform> MessageCampaignPlatform => Set<MessageCampaignPlatform>();
    public DbSet<MessageCampaignCartDiscount> MessageCampaignCartDiscount => Set<MessageCampaignCartDiscount>();
    public DbSet<MessageCampaignLanguageLocation> MessageCampaignLanguageLocation => Set<MessageCampaignLanguageLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LicenseService>().HasOne<LicenseServiceType>().WithMany()
            .HasForeignKey(service => service.LicenseServiceTypeId);
        modelBuilder.Entity<LicenseService>().HasOne<LicenseServiceStatus>().WithMany()
            .HasForeignKey(service => service.LicenseServiceStatusId);
        var currentDateSql = Database.IsSqlServer() ? "GETDATE()" : "CURRENT_TIMESTAMP";
        modelBuilder.Entity<LicenseModuleLicense>().HasOne<License>().WithMany()
            .HasForeignKey(row => row.LicenseId);
        modelBuilder.Entity<LicenseModuleLicense>().HasOne<LicenseModule>().WithMany()
            .HasForeignKey(row => row.LicenseModuleId);
        modelBuilder.Entity<LicenseModuleLicense>().HasOne<LicenseModuleType>().WithMany()
            .HasForeignKey(row => row.LicenseModuleTypeId);
        modelBuilder.Entity<LicenseModuleLicense>().Property(row => row.LastModifiedBy).IsUnicode(false);
        modelBuilder.Entity<LicenseCategoryLicense>().HasIndex(row => row.LicenseId).IsUnique();
        modelBuilder.Entity<LicenseCategoryLicense>().HasOne<License>().WithMany().HasForeignKey(row => row.LicenseId);
        modelBuilder.Entity<LicenseCategoryLicense>().HasOne<LicenseCategory>().WithMany().HasForeignKey(row => row.LicenseCategoryId);
        modelBuilder.Entity<LicenseCategoryLicense>().Property(row => row.LastModifiedBy).IsUnicode(false);
        modelBuilder.Entity<LicenseCapability>().HasIndex(row => new { row.LicenseId, row.CapabilityId }).IsUnique();
        modelBuilder.Entity<LicenseCapability>().HasOne<License>().WithMany().HasForeignKey(row => row.LicenseId);
        modelBuilder.Entity<LicenseCapability>().Property(row => row.InsertBy).IsUnicode(false);
        modelBuilder.Entity<LicenseCapability>().Property(row => row.ModifiedBy).IsUnicode(false);
        modelBuilder.Entity<LicenseSeat>().HasIndex(row => row.LicenseId).IsUnique();
        modelBuilder.Entity<LicenseSeat>().HasOne<License>().WithMany().HasForeignKey(row => row.LicenseId);
        modelBuilder.Entity<LicenseSeat>().Property(row => row.SeatsUsed).HasDefaultValue(0);
        modelBuilder.Entity<LicenseSeat>().Property(row => row.InsertBy).IsUnicode(false);
        modelBuilder.Entity<LicenseSeat>().Property(row => row.ModifiedBy).IsUnicode(false);
        modelBuilder.Entity<LicenseKey>().Property(row => row.LicenseId).ValueGeneratedNever();
        modelBuilder.Entity<LicenseKey>().Property(row => row.Key).HasDefaultValueSql("NEWID()");
        modelBuilder.Entity<LicenseKey>().Property(row => row.SalesforceLicenseId).IsUnicode(false);
        modelBuilder.Entity<LicenseFeatureLicense>().HasOne<License>().WithMany().HasForeignKey(row => row.LicenseId);
        modelBuilder.Entity<LicenseFeatureLicense>().Property(row => row.InsertDate).HasDefaultValueSql(currentDateSql);
        modelBuilder.Entity<LicenseFeatureLicense>().Property(row => row.InsertBy)
            .IsUnicode(false).HasDefaultValueSql(Database.IsSqlServer() ? "SUSER_SNAME()" : "'sqlite-user'");
        modelBuilder.Entity<LicenseFeatureLicense>().Property(row => row.InsertBy).HasSentinel(string.Empty);
        modelBuilder.Entity<LicenseFeatureLicense>().Property(row => row.LicenseFeatureValue).IsUnicode(false);
        modelBuilder.Entity<LicenseService>().Property(service => service.InsertDate)
            .HasColumnType("datetime").HasDefaultValueSql(currentDateSql);
        modelBuilder.Entity<LicenseService>().Property(service => service.ProcessDate)
            .HasColumnType("datetime").HasDefaultValueSql(currentDateSql);
        modelBuilder.Entity<LicenseService>().Property(service => service.UpdateAttempts)
            .HasDefaultValue((byte)0);
        modelBuilder.Entity<LicenseService>().Property(service => service.LicenseServiceStatusId)
            .HasDefaultValue((byte)1);
        modelBuilder.Entity<LicenseServiceType>().Property(type => type.LicenseServiceTypeName).IsUnicode(false);
        modelBuilder.Entity<LicenseServiceType>().Property(type => type.ProcessType).IsUnicode(false);
        modelBuilder.Entity<LicenseServiceStatus>().Property(status => status.LicenseServiceStatusName).IsUnicode(false);

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

        modelBuilder.Entity<LicenseCategoryProductLine>()
            .HasOne(lcpl => lcpl.LicenseCategory)
            .WithMany()
            .HasForeignKey(lcpl => lcpl.LicenseCategoryId);

        modelBuilder.Entity<ProductCapability>()
            .HasOne(pc => pc.Product)
            .WithMany()
            .HasForeignKey(pc => pc.ProductId);

        // product_license_category
        modelBuilder.Entity<ProductLicenseCategory>()
            .HasKey(plc => new { plc.LicenseCategoryId, plc.ProductId });

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
            .HasKey(py => new { py.ProductId, py.Years });

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

        // currency_language_location → currency (many..1)
        modelBuilder.Entity<CurrencyLanguageLocation>()
            .HasOne(cll => cll.Currency)
            .WithMany()
            .HasForeignKey(cll => cll.CurrencyId);
        // keyless projection for dbo.fn_license_select_license_profile output
        modelBuilder.Entity<LicenseProfileFunctionRow>()
            .HasNoKey()
            .ToView(null);

        // keyless table/projection entities
        modelBuilder.Entity<LicenseActiveSeats>()
            .HasNoKey();

        modelBuilder.Entity<LicenseByIdProcedureRow>()
            .HasNoKey()
            .ToView(null);

        // The license.keycode column is varchar(40) in SQL Server, but string properties
        // default to nvarchar in EF Core. Without IsUnicode(false), filtering by keycode
        // sends an nvarchar parameter, forcing SQL Server to implicitly convert every row
        // and scan instead of seeking IX_license_keycode_inc (observed ~7.7s per query).
        modelBuilder.Entity<License>()
            .Property(l => l.Keycode)
            .IsUnicode(false);
        modelBuilder.Entity<License>()
            .Property(license => license.MaxDailyActivations)
            .HasDefaultValue(30);
    }
}
