using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Data.Entities;

[Keyless]
public sealed class LicenseProfileFunctionRow
{
    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("license_id")]
    public int? LicenseId { get; set; }

    [Column("license_category_name")]
    public string? LicenseCategoryName { get; set; }

    [Column("license_category_description")]
    public string? LicenseCategoryDescription { get; set; }

    [Column("license_category_id")]
    public byte? LicenseCategoryId { get; set; }

    [Column("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }

    [Column("license_attribute_id")]
    public int? LicenseAttributeId { get; set; }

    [Column("license_attribute_description")]
    public string? LicenseAttributeDescription { get; set; }

    [Column("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [Column("license_attribute_license_value_description")]
    public string? LicenseAttributeLicenseValueDescription { get; set; }

    [Column("category_type_name")]
    public string? CategoryTypeName { get; set; }

    [Column("category_type_id")]
    public byte? CategoryTypeId { get; set; }

    [Column("license_status_id")]
    public int? LicenseStatusId { get; set; }

    [Column("license_status_description")]
    public string? LicenseStatusDescription { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [Column("license_seats")]
    public int? LicenseSeats { get; set; }

    [Column("storage_gb")]
    public int? StorageGb { get; set; }

    [Column("item_hierarchy_id")]
    public byte? ItemHierarchyId { get; set; }

    [Column("item_hierarchy_name")]
    public string? ItemHierarchyName { get; set; }

    [Column("autorenewal_cycle_name")]
    public string? AutorenewalCycleName { get; set; }

    [Column("autorenewal_cycle")]
    public decimal? AutorenewalCycle { get; set; }

    [Column("usage_pricing_model_id")]
    public byte? UsagePricingModelId { get; set; }

    [Column("usage_pricing_model_name")]
    public string? UsagePricingModelName { get; set; }

    [Column("license_autorenewal_value")]
    public byte? LicenseAutorenewalValue { get; set; }

    [Column("retention_model_id")]
    public byte? RetentionModelId { get; set; }

    [Column("retention_model_name")]
    public string? RetentionModelName { get; set; }

    [Column("retention_model_type_id")]
    public byte? RetentionModelTypeId { get; set; }

    [Column("retention_term")]
    public byte? RetentionTerm { get; set; }

    [Column("product_platform_id")]
    public byte? ProductPlatformId { get; set; }

    [Column("product_platform_name")]
    public string? ProductPlatformName { get; set; }

    [Column("product_pricing_level_id")]
    public byte? ProductPricingLevelId { get; set; }

    [Column("pricing_level")]
    public string? PricingLevel { get; set; }

    [Column("pricing_level_description")]
    public string? PricingLevelDescription { get; set; }

    [Column("license_vault_json")]
    public string? LicenseVaultJson { get; set; }

    [Column("most_recent_order_term")]
    public double? MostRecentOrderTerm { get; set; }
}
