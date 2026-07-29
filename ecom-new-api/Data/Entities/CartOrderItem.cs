using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order_item")]
public sealed class CartOrderItem
{
    [Key]
    [Column("cart_order_item_id")]
    public int CartOrderItemId { get; set; }

    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("invoice_item_in_process_id")]
    public int InvoiceItemInProcessId { get; set; } = 0;  // sentinel: payment workflow

    [Column("vendor_id")]
    public int VendorId { get; set; } = 1;  // 1 = Webroot default

    [Column("line_item")]
    public int LineItem { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("storage_gb")]
    public int? StorageGb { get; set; }

    [Column("list_price")]
    public decimal ListPrice { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("unit_price_pre_vat")]
    public decimal? UnitPricePreVat { get; set; }

    [Column("tax_item_total")]
    public decimal TaxItemTotal { get; set; }

    [Column("usage_price")]
    public decimal? UsagePrice { get; set; }

    [Column("order_item_offer_amount")]
    public decimal? OrderItemOfferAmount { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [Column("cart_item_bundle_id")]
    public int? CartItemBundleId { get; set; }

    [Column("item_hierarchy_id")]
    public byte? ItemHierarchyId { get; set; }

    [Column("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [Column("vendor_order_item_code")]
    [MaxLength(36)]
    public string? VendorOrderItemCode { get; set; }

    [Column("order_item_update_type_id")]
    public byte? OrderItemUpdateTypeId { get; set; }

    [Column("discount")]
    public double? Discount { get; set; }

    [Column("cart_discount_method_id")]
    public byte? CartDiscountMethodId { get; set; }

    [Column("cart_discount_id")]
    public int? CartDiscountId { get; set; }

    [Column("opportunity_line_item_id")]
    [MaxLength(18)]
    public string? OpportunityLineItemId { get; set; }

    [Column("product_locale")]
    [MaxLength(5)]
    public string? ProductLocale { get; set; }

    [Column("tax_exempt")]
    public bool TaxExempt { get; set; }

    [Column("cart_order_status_id")]
    public byte CartOrderStatusId { get; set; }

    [Column("insert_date")]
    public DateTime InsertDate { get; set; }

    [Column("insert_by")]
    [MaxLength(50)]
    public string InsertBy { get; set; } = default!;

    [Column("modified_date")]
    public DateTime ModifiedDate { get; set; }

    [Column("modified_by")]
    [MaxLength(50)]
    public string ModifiedBy { get; set; } = default!;

    // Navigation
    public CartOrder? CartOrder { get; set; }
    public Product? Product { get; set; }
    public CartOrderItemJson? CartOrderItemJson { get; set; }
    public CartOrderItemLicense? CartOrderItemLicense { get; set; }
}
