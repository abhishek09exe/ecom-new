namespace ecom_new_api.Models.Responses;

/// <summary>
/// A single line item in the cart response.
/// Column source: usp_cart_select_cart_order_item SELECT list.
/// All columns are included so the frontend has every field it relies on.
/// </summary>
public sealed class CartOrderItemResponse
{
    // ── Identity ───────────────────────────────────────────────────────────────

    /// <summary>cart_order_item.cart_order_item_id</summary>
    public int CartOrderItemId { get; init; }

    /// <summary>cart_order_item.cart_order_id</summary>
    public int CartOrderId { get; init; }

    /// <summary>Sequential line number within the order.</summary>
    public int LineItem { get; init; }

    // ── Quantity / seats / storage ─────────────────────────────────────────────

    /// <summary>cart_order_item.quantity</summary>
    public int Quantity { get; init; }

    /// <summary>product_seat.seats — number of licence seats per product unit.</summary>
    public int? LicenseSeats { get; set; }

    /// <summary>
    /// Storage in GB. Sourced from cart_order_item.storage_gb, falling back to
    /// fn_get_item_storage_gb() if null in the row.
    /// </summary>
    public int? StorageGb { get; init; }

    /// <summary>product_years.years (DECIMAL 18,3 — supports fractional year terms)</summary>
    public decimal? Years { get; init; }

    // ── Pricing ────────────────────────────────────────────────────────────────

    /// <summary>cart_order_item.order_item_offer_amount — line total after offer/discount.</summary>
    public decimal? OrderItemOfferAmount { get; init; }

    /// <summary>
    /// Equivalent annualised price — computed from product_pricing × years.
    /// NULL for usage-priced (carbonite) or sub-1TB storage items.
    /// </summary>
    public decimal? EquivalentYearPrice { get; init; }

    /// <summary>cart_order_item.list_price — retail/list price before discounts.</summary>
    public decimal? ListPrice { get; init; }

    /// <summary>cart_order_item.unit_price — price per unit after discounts.</summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>cart_order_item.unit_price_pre_vat</summary>
    public decimal? UnitPricePreVat { get; init; }

    /// <summary>cart_order_item.tax_item_total</summary>
    public decimal? TaxItemTotal { get; init; }

    /// <summary>cart_order_item.usage_price — overage / utility usage-based charge.</summary>
    public decimal? UsagePrice { get; init; }

    /// <summary>cart_order_item.discount (FLOAT)</summary>
    public double? Discount { get; init; }

    /// <summary>cart_order_item.cart_discount_method_id</summary>
    public byte? CartDiscountMethodId { get; init; }

    /// <summary>cart_order_item.cart_discount_id</summary>
    public int? CartDiscountId { get; init; }

    // ── Product descriptors ────────────────────────────────────────────────────

    /// <summary>cart_order_item.product_id</summary>
    public int ProductId { get; init; }

    /// <summary>product.product_description</summary>
    public string? ProductDescription { get; init; }

    /// <summary>product_type.product_type_id</summary>
    public int? ProductTypeId { get; init; }

    /// <summary>product_type.product_type_description</summary>
    public string? ProductTypeDescription { get; init; }

    /// <summary>product.license_keycode_type_id</summary>
    public int? LicenseKeycodeTypeId { get; init; }

    /// <summary>license_keycode_type.license_keycode_type_description</summary>
    public string? LicenseKeycodeTypeDescription { get; init; }

    /// <summary>license_category.license_category_id</summary>
    public int? LicenseCategoryId { get; init; }

    /// <summary>license_category.license_category_name</summary>
    public string? LicenseCategoryName { get; init; }

    /// <summary>license_category.license_category_description</summary>
    public string? LicenseCategoryDescription { get; init; }

    /// <summary>product_family.product_family_description</summary>
    public string? ProductFamilyDescription { get; init; }

    /// <summary>product_line.product_line_cart_type</summary>
    public string? ProductLineCartType { get; init; }

    /// <summary>license_category.min_order_quantity</summary>
    public int? MinOrderQuantity { get; init; }

    /// <summary>license_category.max_order_quantity</summary>
    public int? MaxOrderQuantity { get; init; }

    // ── Dates ──────────────────────────────────────────────────────────────────

    /// <summary>cart_order_item.start_date</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>cart_order_item.expiration_date</summary>
    public DateTime? ExpirationDate { get; init; }

    // ── Bundle / hierarchy ─────────────────────────────────────────────────────

    /// <summary>cart_order_item.cart_item_bundle_id — groups primary + secondary items together.</summary>
    public int? CartItemBundleId { get; init; }

    /// <summary>cart_order_item.item_hierarchy_id (1=primary, 2=secondary/add-on)</summary>
    public int? ItemHierarchyId { get; init; }

    /// <summary>
    /// cart_order_item_id of the primary item this secondary depends on.
    /// Derived from a self-join in the SP.
    /// </summary>
    public int? DependentCartOrderItemId { get; init; }

    // ── License / keycode ──────────────────────────────────────────────────────

    /// <summary>cart_order_item_license.keycode</summary>
    public string? Keycode { get; init; }

    /// <summary>cart_order_item.license_attribute_license_value (billing model)</summary>
    public int? LicenseAttributeLicenseValue { get; init; }

    /// <summary>license_attribute_license_value.license_attribute_license_value_description</summary>
    public string? LicenseAttributeLicenseValueDescription { get; init; }

    /// <summary>cart_order_item.vendor_order_item_code</summary>
    public string? VendorOrderItemCode { get; init; }

    /// <summary>cart_order_item.order_item_update_type_id</summary>
    public int? OrderItemUpdateTypeId { get; init; }

    /// <summary>cart_order_item.opportunity_line_item_id (Salesforce 18-char ID)</summary>
    public string? OpportunityLineItemId { get; init; }

    // ── Usage pricing / retention / platform ──────────────────────────────────

    /// <summary>fn_cart_select_cart_order_item_json: usage_pricing_model_id</summary>
    public int? UsagePricingModelId { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: usage_pricing_model_name</summary>
    public string? UsagePricingModelName { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: retention_model_id</summary>
    public int? RetentionModelId { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: retention_model_name</summary>
    public string? RetentionModelName { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: retention_term</summary>
    public int? RetentionTerm { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: retention_model_type_id</summary>
    public int? RetentionModelTypeId { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: product_platform_id</summary>
    public int? ProductPlatformId { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: product_platform_name</summary>
    public string? ProductPlatformName { get; init; }

    // ── Vault ──────────────────────────────────────────────────────────────────

    /// <summary>fn_cart_select_cart_order_item_json: vault_id</summary>
    public int? VaultId { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: vault_datacenter_name</summary>
    public string? VaultDatacenterName { get; init; }

    /// <summary>
    /// fn_cart_select_cart_order_item_json: vault — JSON array of vault objects
    /// (supports multiple vaults added in 2020-09-28).
    /// </summary>
    public string? Vault { get; init; }

    // ── Pricing level ──────────────────────────────────────────────────────────

    /// <summary>fn_cart_select_cart_order_item_json: product_pricing_level_id</summary>
    public int? ProductPricingLevelId { get; init; }

    /// <summary>fn_cart_select_cart_order_item_json: pricing_level_description</summary>
    public string? PricingLevelDescription { get; init; }

    // ── Raw JSON ───────────────────────────────────────────────────────────────

    /// <summary>cart_order_item_json.cart_order_item_json — raw item-level extension JSON.</summary>
    public string? CartOrderItemJson { get; init; }

    // ── Computed sub-totals (quantity × unit price) ─────────────────────────────────

    /// <summary>list_price × quantity</summary>
    public decimal? SubTotalListAmount { get; set; }

    /// <summary>unit_price × quantity</summary>
    public decimal? SubTotalAmount { get; set; }

    /// <summary>unit_price_pre_vat × quantity</summary>
    public decimal? SubTotalAmountPreVat { get; set; }

    /// <summary>equivalent_year_price × quantity</summary>
    public decimal? SubTotalEquivalentYearPrice { get; set; }

    /// <summary>Estimated monthly price for usage-based pricing. Null for annual plans.</summary>
    public decimal? EstimatedMonthlyPrice { get; set; }

    // ── Formatted price strings ───────────────────────────────────────────────────────────

    public string? EquivalentYearPriceFmt { get; set; }
    public string? ListPriceFmt { get; set; }
    public string? UnitPriceFmt { get; set; }
    public string? UnitPricePreVatFmt { get; set; }
    public string? UsagePriceFmt { get; set; }
    public string? SubTotalEquivalentYearPriceFmt { get; set; }
    public string? SubTotalListAmountFmt { get; set; }
    public string? SubTotalAmountFmt { get; set; }
    public string? SubTotalAmountPreVatFmt { get; set; }

    // ── Upsells ───────────────────────────────────────────────────────────────────────

    /// <summary>Upsell offers for this item. Empty array if none.</summary>
    public List<object> Upsells { get; set; } = [];

    /// <summary>Upsell modal data. Empty array if none.</summary>
    public List<object> UpsellModal { get; set; } = [];

    // ── Complex computed objects (require additional DB lookups) ───────────────────

    /// <summary>
    /// License profile keyed by license_category_name.
    /// TODO: Populate from usp_cart_select_license_profile equivalent.
    /// </summary>
    public object? LicenseProfile { get; set; }

    /// <summary>
    /// Available options (years_list, license_category_list, pricing_level_list, vault).
    /// TODO: Populate from product metadata queries.
    /// </summary>
    public object? Options { get; set; }

    /// <summary>
    /// Product features list.
    /// TODO: Populate from product_feature joins.
    /// </summary>
    public List<string>? ProductFeatureList { get; set; }

    // ── Message key (order-level key propagated to each item) ──────────────────────

    /// <summary>Message key from the order's cart_order_message record.</summary>
    public string? MessageKey { get; set; }
}

