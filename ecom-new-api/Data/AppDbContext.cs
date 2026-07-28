using ecom_new_api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Data;

/// <summary>
/// EF Core DbContext for the cart database.
///
/// Each DbSet maps to a real table. EF Core generates all SQL — no stored procedures.
/// Navigation properties on the entities replace the manual JOINs the old SPs did.
///
/// Table/column names are mapped explicitly so the C# names (PascalCase) can differ
/// from the SQL names (snake_case) without any mismatch.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CartOrder> CartOrders => Set<CartOrder>();
    public DbSet<CartOrderItem> CartOrderItems => Set<CartOrderItem>();
    public DbSet<CartOrderPartner> CartOrderPartners => Set<CartOrderPartner>();
    public DbSet<CartJson> CartJsons => Set<CartJson>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CartOrderStatus> CartOrderStatuses => Set<CartOrderStatus>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<LicenseCategory> LicenseCategories => Set<LicenseCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartSiteIdOrderCodePrefix> CartSiteIdOrderCodePrefixes => Set<CartSiteIdOrderCodePrefix>();
    public DbSet<CartOrderRoute> CartOrderRoutes => Set<CartOrderRoute>();
    public DbSet<CartOrderMessage> CartOrderMessages => Set<CartOrderMessage>();
    public DbSet<LicenseKey> LicenseKeys => Set<LicenseKey>();
    public DbSet<PartnerConfigurationPartner> PartnerConfigurationPartners => Set<PartnerConfigurationPartner>();
    public DbSet<CartOrderItemJson> CartOrderItemJsons => Set<CartOrderItemJson>();
    public DbSet<CartOrderItemLicense> CartOrderItemLicenses => Set<CartOrderItemLicense>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // ── currency ──────────────────────────────────────────────────────────
        m.Entity<Currency>(e =>
        {
            e.ToTable("currency");
            e.HasKey(x => x.CurrencyId);
            e.Property(x => x.CurrencyId).HasColumnName("currency_id");
            e.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength();
            e.Property(x => x.CurrencyDescription).HasColumnName("currency_description").HasMaxLength(20);
            e.Property(x => x.SymbolHtml).HasColumnName("symbol_html").HasMaxLength(10);
            e.Property(x => x.SymbolUtf8).HasColumnName("symbol_utf8").HasMaxLength(10);
            e.Property(x => x.SymbolText).HasColumnName("symbol_text").HasMaxLength(10);
            e.Property(x => x.ExchangeRate).HasColumnName("exchange_rate");
            e.Property(x => x.ExchangeMultiplier).HasColumnName("exchange_multiplier");
            e.Property(x => x.DrLocale).HasColumnName("dr_locale").HasMaxLength(10);
            e.Property(x => x.Active).HasColumnName("active");
        });

        // ── cart_order_status ─────────────────────────────────────────────────
        m.Entity<CartOrderStatus>(e =>
        {
            e.ToTable("cart_order_status");
            e.HasKey(x => x.CartOrderStatusId);
            e.Property(x => x.CartOrderStatusId).HasColumnName("cart_order_status_id");
            e.Property(x => x.StatusDescription).HasColumnName("cart_order_status_description").HasMaxLength(50);
            e.Property(x => x.InsertDate).HasColumnName("insert_date");
            e.Property(x => x.InsertBy).HasColumnName("insert_by").HasMaxLength(200);
        });

        // ── partner ───────────────────────────────────────────────────────────
        m.Entity<Partner>(e =>
        {
            e.ToTable("partner");
            e.HasKey(x => x.PartnerId);
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(100);
            e.Property(x => x.PartnerTypeId).HasColumnName("partner_type_id");
            e.Property(x => x.PartnerStatusId).HasColumnName("partner_status_id").HasDefaultValue((byte)1);
            e.Property(x => x.PartnerKey).HasColumnName("partner_key");
            e.Property(x => x.ParentPartnerId).HasColumnName("parent_partner_id");
            e.Property(x => x.SalesforceId).HasColumnName("salesforce_id").HasMaxLength(20);
            e.Property(x => x.OracleId).HasColumnName("oracle_id").HasMaxLength(20);
            e.Property(x => x.AccountOwnerId).HasColumnName("account_owner_id").HasMaxLength(18);
        });

        // ── license_category ──────────────────────────────────────────────────
        m.Entity<LicenseCategory>(e =>
        {
            e.ToTable("license_category");
            e.HasKey(x => x.LicenseCategoryId);
            e.Property(x => x.LicenseCategoryId).HasColumnName("license_category_id");
            e.Property(x => x.LicenseCategoryName).HasColumnName("license_category_name").HasMaxLength(50);
            e.Property(x => x.LicenseCategoryDescription).HasColumnName("license_category_description").HasMaxLength(100);
            e.Property(x => x.MinOrderQuantity).HasColumnName("min_order_quantity");
            e.Property(x => x.MaxOrderQuantity).HasColumnName("max_order_quantity");
        });

        // ── product ───────────────────────────────────────────────────────────
        m.Entity<Product>(e =>
        {
            e.ToTable("product");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.ProductDescription).HasColumnName("product_description").HasMaxLength(100);
            e.Property(x => x.ProductTypeId).HasColumnName("product_type_id");
            e.Property(x => x.ProductFamilyId).HasColumnName("product_family_id");
            e.Property(x => x.ProductLifecycleId).HasColumnName("product_lifecycle_id").HasDefaultValue(1);
            e.Property(x => x.LicenseKeycodeTypeId).HasColumnName("license_keycode_type_id");
            e.Property(x => x.RootProductId).HasColumnName("root_product_id");
            e.Property(x => x.UsesKeycode).HasColumnName("uses_keycode").HasDefaultValue(0);
            e.Property(x => x.CdProductId).HasColumnName("cd_product_id").HasDefaultValue(0);
            e.Property(x => x.RetailPrice).HasColumnName("retail_price").HasColumnType("decimal(18,4)");
            e.Property(x => x.Basename).HasColumnName("basename").HasMaxLength(32);
        });

        // ── cart_order ────────────────────────────────────────────────────────
        m.Entity<CartOrder>(e =>
        {
            e.ToTable("cart_order");
            e.HasKey(x => x.CartOrderId);
            e.Property(x => x.CartOrderId).HasColumnName("cart_order_id").ValueGeneratedOnAdd();
            e.Property(x => x.CartCustomerId).HasColumnName("cart_customer_id").HasDefaultValue(0);
            e.Property(x => x.InvoiceInProcessId).HasColumnName("invoice_in_process_id").HasDefaultValue(0);
            e.Property(x => x.VendorOrderCode).HasColumnName("vendor_order_code").HasMaxLength(100);
            e.Property(x => x.OrderType).HasColumnName("order_type").HasMaxLength(30);
            e.Property(x => x.SiteId).HasColumnName("site_id").HasMaxLength(65);
            e.Property(x => x.SiteUrl).HasColumnName("site_url").HasMaxLength(1025);
            e.Property(x => x.PRc).HasColumnName("p_rc").HasMaxLength(50).HasDefaultValue("1");
            e.Property(x => x.PRsc).HasColumnName("p_rsc").HasMaxLength(50);
            e.Property(x => x.PAc).HasColumnName("p_ac").HasMaxLength(100);
            e.Property(x => x.TrxRc).HasColumnName("trx_rc").HasMaxLength(50);
            e.Property(x => x.TrxRsc).HasColumnName("trx_rsc").HasMaxLength(50);
            e.Property(x => x.TrxAc).HasColumnName("trx_ac").HasMaxLength(100);
            e.Property(x => x.Aid).HasColumnName("aid").HasMaxLength(50);
            e.Property(x => x.Pid).HasColumnName("pid").HasMaxLength(50);
            e.Property(x => x.Sid).HasColumnName("sid").HasMaxLength(100);
            e.Property(x => x.OfferId).HasColumnName("offer_id").HasMaxLength(65);
            e.Property(x => x.OfferAmount).HasColumnName("offer_amount").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.SubTotalAmount).HasColumnName("sub_total_amount").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255).HasDefaultValue(string.Empty);
            e.Property(x => x.ExchangeRate).HasColumnName("exchange_rate").HasColumnType("decimal(18,4)");
            e.Property(x => x.SessionId).HasColumnName("session_id").HasDefaultValue(0L);
            e.Property(x => x.SubmissionDate).HasColumnName("submission_date");
            e.Property(x => x.SalesOrderDate).HasColumnName("sales_order_date");
            e.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(5).IsFixedLength();
            e.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255);
            e.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(8000);
            e.Property(x => x.InsertDate).HasColumnName("insert_date");
            e.Property(x => x.InsertBy).HasColumnName("insert_by").HasMaxLength(50);
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by").HasMaxLength(50);
            e.Property(x => x.CartOrderStatusId).HasColumnName("cart_order_status_id").HasDefaultValue((byte)1);
            e.Property(x => x.CurrencyId).HasColumnName("currency_id");
            e.Property(x => x.CustomerProfileToken).HasColumnName("customer_profile_token").HasMaxLength(24);
            e.Property(x => x.CartOrderInProcessId).HasColumnName("cart_order_in_process_id");
            e.Property(x => x.UserIp).HasColumnName("user_ip").HasMaxLength(16);
            e.Property(x => x.Restriction).HasColumnName("restriction").HasMaxLength(20);

            e.HasOne(x => x.Currency)
             .WithMany(c => c.CartOrders)
             .HasForeignKey(x => x.CurrencyId);

            e.HasOne(x => x.CartOrderStatus)
             .WithMany(s => s.CartOrders)
             .HasForeignKey(x => x.CartOrderStatusId);

            e.HasMany(x => x.Items)
             .WithOne(i => i.CartOrder)
             .HasForeignKey(i => i.CartOrderId);

            e.HasOne(x => x.CartOrderPartner)
             .WithOne(p => p.CartOrder)
             .HasForeignKey<CartOrderPartner>(p => p.CartOrderId);

            e.HasOne(x => x.CartJson)
             .WithOne(j => j.CartOrder)
             .HasForeignKey<CartJson>(j => j.CartOrderId);
        });

        // ── cart_order_partner ────────────────────────────────────────────────
        m.Entity<CartOrderPartner>(e =>
        {
            e.ToTable("cart_order_partner");
            e.HasKey(x => x.CartOrderPartnerId);
            e.Property(x => x.CartOrderPartnerId).HasColumnName("cart_order_partner_id").ValueGeneratedOnAdd();
            e.Property(x => x.CartOrderId).HasColumnName("cart_order_id");
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.Property(x => x.PartnerAccountId).HasColumnName("partner_account_id");

            e.HasOne(x => x.Partner)
             .WithMany(p => p.CartOrderPartners)
             .HasForeignKey(x => x.PartnerId);
        });

        // ── cart_json ─────────────────────────────────────────────────────────
        m.Entity<CartJson>(e =>
        {
            e.ToTable("cart_json");
            e.HasKey(x => x.CartJsonId);
            e.Property(x => x.CartJsonId).HasColumnName("cart_json_id").ValueGeneratedOnAdd();
            e.Property(x => x.Json).HasColumnName("cart_json");
            e.Property(x => x.CartOrderId).HasColumnName("cart_order_id");
            e.Property(x => x.CartOrderInProcessId).HasColumnName("cart_order_in_process_id");
        });

        // ── cart_order_item ───────────────────────────────────────────────────
        m.Entity<CartOrderItem>(e =>
        {
            e.ToTable("cart_order_item");
            e.HasKey(x => x.CartOrderItemId);
            e.Property(x => x.CartOrderItemId).HasColumnName("cart_order_item_id").ValueGeneratedOnAdd();
            e.Property(x => x.CartOrderId).HasColumnName("cart_order_id");
            e.Property(x => x.InvoiceItemInProcessId).HasColumnName("invoice_item_in_process_id").HasDefaultValue(0);
            e.Property(x => x.VendorId).HasColumnName("vendor_id").HasDefaultValue(1);
            e.Property(x => x.LineItem).HasColumnName("line_item");
            e.Property(x => x.VendorProductId).HasColumnName("vendor_product_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.OrderItemOfferCode).HasColumnName("order_item_offer_code");
            e.Property(x => x.OrderItemOfferAmount).HasColumnName("order_item_offer_amount").HasColumnType("decimal(18,4)");
            e.Property(x => x.ListPrice).HasColumnName("list_price").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.TaxItemTotal).HasColumnName("tax_item_total").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.TaxExempt).HasColumnName("tax_exempt").HasDefaultValue(false);
            e.Property(x => x.ConversionProductId).HasColumnName("conversion_product_id");
            e.Property(x => x.ProductLocale).HasColumnName("product_locale").HasMaxLength(5);
            e.Property(x => x.UnitPricePreVat).HasColumnName("unit_price_pre_vat").HasColumnType("decimal(18,4)");
            e.Property(x => x.UsagePrice).HasColumnName("usage_price").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(x => x.Discount).HasColumnName("discount");
            e.Property(x => x.CartDiscountMethodId).HasColumnName("cart_discount_method_id");
            e.Property(x => x.CartDiscountId).HasColumnName("cart_discount_id");
            e.Property(x => x.CartOrderStatusId).HasColumnName("cart_order_status_id").HasDefaultValue((byte)1);
            e.Property(x => x.CartOrderItemInProcessId).HasColumnName("cart_order_item_in_process_id");
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.ExpirationDate).HasColumnName("expiration_date");
            e.Property(x => x.CartItemBundleId).HasColumnName("cart_item_bundle_id");
            e.Property(x => x.ItemHierarchyId).HasColumnName("item_hierarchy_id").HasDefaultValue((byte)1);
            e.Property(x => x.LicenseAttributeLicenseValue).HasColumnName("license_attribute_license_value");
            e.Property(x => x.VendorOrderItemCode).HasColumnName("vendor_order_item_code").HasMaxLength(36);
            e.Property(x => x.OrderItemUpdateTypeId).HasColumnName("order_item_update_type_id").HasDefaultValue((byte)1);
            e.Property(x => x.OpportunityLineItemId).HasColumnName("opportunity_line_item_id").HasMaxLength(18);
            e.Property(x => x.SapMaterialNumber).HasColumnName("sap_material_number");
            e.Property(x => x.StorageGb).HasColumnName("storage_gb");
            e.Property(x => x.InsertDate).HasColumnName("insert_date");
            e.Property(x => x.InsertBy).HasColumnName("insert_by").HasMaxLength(100);
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);

            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId);
        });

        // ── cart_site_id_order_code_prefix ────────────────────────────────────
        m.Entity<CartSiteIdOrderCodePrefix>(e =>
        {
            e.ToTable("cart_site_id_order_code_prefix");
            e.HasKey(x => x.CartSiteIdOrderCodePrefixId);
            e.Property(x => x.CartSiteIdOrderCodePrefixId).HasColumnName("cart_site_id_order_code_prefix_id").ValueGeneratedOnAdd();
            e.Property(x => x.SiteId).HasColumnName("site_id").HasMaxLength(65).IsRequired();
            e.Property(x => x.VendorOrderCodePrefix).HasColumnName("vendor_order_code_prefix").HasMaxLength(5).IsRequired();
            e.Property(x => x.SiteIdDescription).HasColumnName("site_id_description").HasMaxLength(100);
        });

        // ── cart_order_route (G4) ─────────────────────────────────────────────
        m.Entity<CartOrderRoute>(e =>
        {
            e.ToTable("cart_order_route");
            e.HasKey(x => x.CartOrderRouteId);
            e.Property(x => x.CartOrderRouteId).HasColumnName("cart_order_route_id").ValueGeneratedOnAdd();
            e.Property(x => x.CartOrderId).HasColumnName("cart_order_id");
            e.Property(x => x.RoutingAction).HasColumnName("routing_action").HasMaxLength(50).IsRequired();
            e.Property(x => x.InsertDate).HasColumnName("insert_date");
            e.HasOne(x => x.CartOrder).WithMany().HasForeignKey(x => x.CartOrderId);
        });

        // ── cart_order_message (G5) ───────────────────────────────────────────
        m.Entity<CartOrderMessage>(e =>
        {
            e.ToTable("cart_order_message");
            e.HasKey(x => x.CartOrderMessageId);
            e.Property(x => x.CartOrderMessageId).HasColumnName("cart_order_message_id").ValueGeneratedOnAdd();
            e.Property(x => x.CartOrderId).HasColumnName("cart_order_id");
            e.Property(x => x.MessageKey).HasColumnName("message_key");
            e.Property(x => x.LicenseId).HasColumnName("license_id");
            e.Property(x => x.CartDiscountId).HasColumnName("cart_discount_id");
            e.Property(x => x.StatusId).HasColumnName("status_id").HasDefaultValue((byte)1);
            e.Property(x => x.MessageCampaignId).HasColumnName("message_campaign_id");
            e.Property(x => x.MessageCampaignPlatform).HasColumnName("message_campaign_platform").HasMaxLength(50);
            e.HasOne(x => x.CartOrder).WithMany().HasForeignKey(x => x.CartOrderId);
        });

        // ── license_key (G5 lookup — read-only) ──────────────────────────────
        m.Entity<LicenseKey>(e =>
        {
            e.ToTable("license_key");
            e.HasKey(x => x.LicenseKeyId);
            e.Property(x => x.LicenseKeyId).HasColumnName("license_key_id").ValueGeneratedOnAdd();
            e.Property(x => x.LicenseKeyValue).HasColumnName("license_key");
            e.Property(x => x.LicenseId).HasColumnName("license_id");
            e.Property(x => x.SalesforceLicenseId).HasColumnName("salesforce_license_id").HasMaxLength(50);
        });

        // ── partner_configuration_partner (G6) ────────────────────────────────
        m.Entity<PartnerConfigurationPartner>(e =>
        {
            e.ToTable("partner_configuration_partner");
            e.HasKey(x => x.PartnerConfigurationPartnerId);
            e.Property(x => x.PartnerConfigurationPartnerId).HasColumnName("partner_configuration_partner_id").ValueGeneratedOnAdd();
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.Property(x => x.PartnerConfigurationId).HasColumnName("partner_configuration_id");
            e.Property(x => x.ConfigurationValue).HasColumnName("configuration_value").HasMaxLength(100).IsRequired();
        });

        // ── cart_order_item_json (G8) ─────────────────────────────────────────
        m.Entity<CartOrderItemJson>(e =>
        {
            e.ToTable("cart_order_item_json");
            e.HasKey(x => x.CartOrderItemJsonId);
            e.Property(x => x.CartOrderItemJsonId).HasColumnName("cart_order_item_json_id").ValueGeneratedOnAdd();
            e.Property(x => x.CartOrderItemId).HasColumnName("cart_order_item_id");
            e.Property(x => x.CartOrderItemJsonValue).HasColumnName("cart_order_item_json").IsRequired();
            e.Property(x => x.InsertDate).HasColumnName("insert_date");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");
            e.HasOne(x => x.CartOrderItem).WithMany().HasForeignKey(x => x.CartOrderItemId);
        });

        // ── cart_order_item_license (G9) ──────────────────────────────────────
        m.Entity<CartOrderItemLicense>(e =>
        {
            e.ToTable("cart_order_item_license");
            e.HasKey(x => x.CartOrderItemLicenseId);
            e.Property(x => x.CartOrderItemLicenseId).HasColumnName("cart_order_item_license_id").ValueGeneratedOnAdd();
            e.Property(x => x.CartOrderItemId).HasColumnName("cart_order_item_id");
            e.Property(x => x.Keycode).HasColumnName("keycode").HasMaxLength(40).IsRequired();
            e.Property(x => x.InsertDate).HasColumnName("insert_date");
            e.Property(x => x.InsertBy).HasColumnName("insert_by").HasMaxLength(100).IsRequired();
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100).IsRequired();
            e.Property(x => x.CartOrderStatusId).HasColumnName("cart_order_status_id").HasDefaultValue((byte)1);
            e.HasOne(x => x.CartOrderItem).WithMany().HasForeignKey(x => x.CartOrderItemId);
        });
    }
}
