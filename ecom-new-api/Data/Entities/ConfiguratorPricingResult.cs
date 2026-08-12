using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// POCO mapped from usp_cart_select_license_configurator_pricing result set.
/// Not an EF entity — used with Database.SqlQueryRaw&lt;T&gt;.
/// </summary>
public class ConfiguratorPricingResult
{
    [Column("line_item")]
    public int LineItem { get; set; }
    [Column("quantity")]
    public int Quantity { get; set; }
    [Column("list_price")]
    public decimal ListPrice { get; set; }
    [Column("unit_price")]
    public decimal UnitPrice { get; set; }
    [Column("usage_price")]
    public decimal UsagePrice { get; set; }
    [Column("equivalent_year_price")]
    public decimal? EquivalentYearPrice { get; set; }
    [Column("order_item_offer_amount")]
    public decimal? OrderItemOfferAmount { get; set; }
    [Column("product_description")]
    public string ProductDescription { get; set; } = string.Empty;
    [Column("product_type_description")]
    public string ProductTypeDescription { get; set; } = string.Empty;
    [Column("license_category_name")]
    public string LicenseCategoryName { get; set; } = string.Empty;
    [Column("license_category_description")]
    public string? LicenseCategoryDescription { get; set; }
    [Column("product_family_description")]
    public string? ProductFamilyDescription { get; set; }
    [Column("start_date")]
    public DateTime? StartDate { get; set; }
    [Column("expiration_date")]
    public DateTime? ExpirationDate { get; set; }
    [Column("cart_item_bundle_id")]
    public int CartItemBundleId { get; set; }
    [Column("item_hierarchy_id")]
    public byte ItemHierarchyId { get; set; }
    [Column("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }
    [Column("dependent_cart_order_item_id")]
    public int? DependentCartOrderItemId { get; set; }
    [Column("storage_gb")]
    public int? StorageGb { get; set; }
    [Column("usage_pricing_model_id")]
    public int? UsagePricingModelId { get; set; }
    [Column("retention_model_id")]
    public int? RetentionModelId { get; set; }
    [Column("retention_term")]
    public string? RetentionTerm { get; set; }
    [Column("retention_model_name")]
    public string? RetentionModelName { get; set; }
    [Column("actual_storage_quantity")]
    public decimal? ActualStorageQuantity { get; set; }
}
