using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a line item in a cart order
/// Maps to [ecommerce_VH14].[dbo].[cart_order_item]
/// </summary>
[Table("cart_order_item")]
public class CartOrderItemEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("cart_order_item_id")]
    public int CartOrderItemId { get; set; }

    [Required]
    [Column("cart_order_id")]
    [ForeignKey("CartOrder")]
    public int CartOrderId { get; set; }

    [Column("line_item")]
    public int? LineItem { get; set; }

    [Column("quantity")]
    public int? Quantity { get; set; }

    [Column("seats")]
    public int? Seats { get; set; }

    [Column("storage_gb")]
    public int? StorageGb { get; set; }

    [Column("order_item_offer_amount", TypeName = "money")]
    public decimal? OrderItemOfferAmount { get; set; }

    [Column("list_price", TypeName = "money")]
    public decimal? ListPrice { get; set; }

    [Column("unit_price", TypeName = "money")]
    public decimal? UnitPrice { get; set; }

    [Column("unit_price_pre_vat", TypeName = "money")]
    public decimal? UnitPricePreVat { get; set; }

    [Column("tax_item_total", TypeName = "money")]
    public decimal? TaxItemTotal { get; set; }

    [Column("usage_price", TypeName = "money")]
    public decimal? UsagePrice { get; set; }

    [Column("discount")]
    public double? Discount { get; set; }

    [Column("cart_discount_method_id")]
    public byte? CartDiscountMethodId { get; set; }

    [Column("cart_discount_id")]
    public int? CartDiscountId { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [Column("cart_item_bundle_id")]
    public int? CartItemBundleId { get; set; }

    [Column("item_hierarchy_id")]
    public byte? ItemHierarchyId { get; set; }

    [MaxLength(40)]
    [Column("keycode")]
    public string? Keycode { get; set; }

    [Column("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [MaxLength(36)]
    [Column("vendor_order_item_code")]
    public string? VendorOrderItemCode { get; set; }

    [Column("order_item_update_type_id")]
    public int? OrderItemUpdateTypeId { get; set; }

    [MaxLength(18)]
    [Column("opportunity_line_item_id")]
    public string? OpportunityLineItemId { get; set; }

    [Column("usage_pricing_model_id")]
    public int? UsagePricingModelId { get; set; }

    [Column("retention_model_id")]
    public int? RetentionModelId { get; set; }

    [Column("retention_term")]
    public int? RetentionTerm { get; set; }

    [Column("product_platform_id")]
    public int? ProductPlatformId { get; set; }

    [Column("product_id")]
    [ForeignKey("Product")]
    public int? ProductId { get; set; }

    [Column("vault_id")]
    public int? VaultId { get; set; }

    [MaxLength(500)]
    [Column("vault_datacenter_name")]
    public string? VaultDatacenterName { get; set; }

    [Column("vault")]
    public string? Vault { get; set; }

    [Column("insert_date")]
    public DateTime? InsertDate { get; set; }

    [MaxLength(256)]
    [Column("insert_by")]
    public string? InsertBy { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }

    [MaxLength(256)]
    [Column("modified_by")]
    public string? ModifiedBy { get; set; }

    [Column("sap_material_number")]
    public int? SapMaterialNumber { get; set; }

    // Navigation properties
    public virtual CartOrderEntity CartOrder { get; set; } = null!;
    public virtual ProductEntity? Product { get; set; }
    public virtual CartOrderItemJsonEntity? CartOrderItemJson { get; set; }
    public virtual ICollection<CartOrderItemLicenseEntity> CartOrderItemLicenses { get; set; } = [];
}
