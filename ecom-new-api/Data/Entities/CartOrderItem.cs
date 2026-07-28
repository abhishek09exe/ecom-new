namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to the cart_order_item table.
/// One row per line item in an order.
/// Columns verified against real QA DB schema.
/// </summary>
public sealed class CartOrderItem
{
    public int CartOrderItemId { get; set; }
    public int CartOrderId { get; set; }
    public int InvoiceItemInProcessId { get; set; }       // NOT NULL DEFAULT 0
    public int VendorId { get; set; }                     // NOT NULL DEFAULT 1
    public int LineItem { get; set; }
    public int? VendorProductId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? OrderItemOfferCode { get; set; }
    public decimal? OrderItemOfferAmount { get; set; }
    public decimal ListPrice { get; set; }                // NOT NULL DEFAULT 0
    public decimal UnitPrice { get; set; }                // NOT NULL DEFAULT 0
    public decimal TaxItemTotal { get; set; }             // NOT NULL DEFAULT 0
    public bool TaxExempt { get; set; }                   // NOT NULL DEFAULT 0
    public int? ConversionProductId { get; set; }
    public string? ProductLocale { get; set; }
    public decimal? UnitPricePreVat { get; set; }
    public decimal? UsagePrice { get; set; }
    public double? Discount { get; set; }
    public byte? CartDiscountMethodId { get; set; }
    public int? CartDiscountId { get; set; }
    public byte CartOrderStatusId { get; set; }           // NOT NULL DEFAULT 1
    public int? CartOrderItemInProcessId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? CartItemBundleId { get; set; }
    public byte? ItemHierarchyId { get; set; }            // tinyint in real DB
    public int? LicenseAttributeLicenseValue { get; set; }
    public string? VendorOrderItemCode { get; set; }
    public byte? OrderItemUpdateTypeId { get; set; }      // tinyint in real DB
    public string? OpportunityLineItemId { get; set; }
    public int? SapMaterialNumber { get; set; }
    public int? StorageGb { get; set; }
    public DateTime InsertDate { get; set; }
    public string? InsertBy { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────────

    /// <summary>Parent order.</summary>
    public CartOrder CartOrder { get; set; } = default!;

    /// <summary>Product lookup — gives ProductDescription on the response.</summary>
    public Product? Product { get; set; }

    /// <summary>cart_order_item_license row — provides Keycode on the response.</summary>
    public CartOrderItemLicense? ItemLicense { get; set; }

    /// <summary>cart_order_item_json row — provides vault, retention, platform fields.</summary>
    public CartOrderItemJson? ItemJson { get; set; }

    /// <summary>license_attribute_license_value lookup — provides description on the response.</summary>
    public LicenseAttributeLicenseValue? LicenseAttributeValue { get; set; }
}
