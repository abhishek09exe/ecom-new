namespace ecom_new_api.Models.Requests;

/// <summary>
/// A single line item within a cart order create request.
///
/// Field names and types are sourced directly from the OPENJSON mappings in:
///   - usp_cart_insert_cart_order_item  → @item_json
///   - usp_cart_insert_cart_order_item  → @bundle_json
///
/// Both JSON blobs are constructed by the service layer and passed to the SP together.
/// </summary>
public sealed class CartOrderItemRequest
{
    // ── item_json fields ──────────────────────────────────────────────────────

    /// <summary>
    /// License category name (e.g. "SOHO", "SMB", "ENT", "OTSF").
    /// Maps to: @item_json → $.license_category_name (VARCHAR 10).
    /// Must be in the allowed set — validated in CartOrderService.
    /// TODO: REPLACE WITH ACTUAL — load allowed set from DB/config.
    /// </summary>
    public string LicenseCategoryName { get; init; } = default!;

    /// <summary>
    /// Internal product identifier.
    /// Maps to: @item_json → $.product_id (INT).
    /// </summary>
    public int ProductId { get; init; }

    /// <summary>
    /// Number of units ordered. Must be positive if provided.
    /// Maps to: @item_json → $.quantity (INT).
    /// </summary>
    public int? Quantity { get; init; }

    /// <summary>
    /// Number of license seats. Must be positive if provided.
    /// Maps to: @item_json → $.license_seats (INT).
    /// Note: SP stores this as both quantity and total_license_seats initially.
    /// </summary>
    public int? LicenseSeats { get; init; }

    /// <summary>
    /// Cloud storage in GB.
    /// Maps to: @item_json → $.storage_gb (INT).
    /// TODO: REPLACE WITH ACTUAL — validate against product max via DB lookup.
    /// </summary>
    public int? StorageGb { get; init; }

    /// <summary>
    /// Subscription term in years. Stored as DECIMAL(18,3) in the SP to support fractional years.
    /// Maps to: @item_json → $.years (DECIMAL 18,3).
    /// Must be in the allowed year set if provided.
    /// TODO: REPLACE WITH ACTUAL — load allowed set from DB/config.
    /// </summary>
    public decimal? Years { get; init; }

    /// <summary>
    /// License keycode type identifier.
    /// Maps to: @item_json → $.license_keycode_type_id (INT)
    ///      and @bundle_json → $.license_keycode_type_id.
    /// </summary>
    public int? LicenseKeycodeTypeId { get; init; }

    /// <summary>
    /// Per-item locale override. Falls back to cart locale if omitted.
    /// Maps to: @item_json → $.locale (VARCHAR 5).
    /// </summary>
    public string? Locale { get; init; }

    /// <summary>
    /// Billing model identifier (e.g. annual, monthly, overage).
    /// Maps to: @item_json → $.license_attribute_license_value (INT)
    ///      and @bundle_json → $.license_attribute_license_value.
    /// </summary>
    public int? LicenseAttributeLicenseValue { get; init; }

    /// <summary>License start date override. Maps to: @item_json → $.start_date (DATETIME).</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>License expiration date override. Maps to: @item_json → $.expiration_date (DATETIME).</summary>
    public DateTime? ExpirationDate { get; init; }

    /// <summary>
    /// Groups related items (primary + secondary) into a logical bundle.
    /// Maps to: @item_json → $.cart_item_bundle_id (INT).
    /// </summary>
    public int? CartItemBundleId { get; init; }

    /// <summary>
    /// 1 = primary product, 2 = secondary/add-on. Must be 1 or 2.
    /// Maps to: @item_json → $.item_hierarchy_id (INT).
    /// </summary>
    public int? ItemHierarchyId { get; init; }

    /// <summary>
    /// External vendor line item code (e.g. Salesforce opportunity line item id format).
    /// Maps to: @item_json → $.vendor_order_item_code (VARCHAR 36).
    /// </summary>
    public string? VendorOrderItemCode { get; init; }

    /// <summary>
    /// Percentage or fixed discount to apply. FLOAT in the SP.
    /// Maps to: @item_json → $.discount (FLOAT).
    /// </summary>
    public double? Discount { get; init; }

    /// <summary>
    /// Identifies the discount calculation method.
    /// Maps to: @item_json → $.cart_discount_method_id (TINYINT).
    /// </summary>
    public byte? CartDiscountMethodId { get; init; }

    /// <summary>Vendor-reported license expiration date. Maps to: @item_json → $.vendor_expiration_date (DATE).</summary>
    public DateOnly? VendorExpirationDate { get; init; }

    /// <summary>
    /// Usage pricing model (e.g. utility / overage / carbonite).
    /// Maps to: @item_json → $.usage_pricing_model_id (TINYINT).
    /// </summary>
    public byte? UsagePricingModelId { get; init; }

    /// <summary>
    /// Salesforce opportunity line item reference (18-char SFDC ID).
    /// Maps to: @item_json → $.opportunity_line_item_id (VARCHAR 18).
    /// </summary>
    public string? OpportunityLineItemId { get; init; }

    /// <summary>
    /// Pre-negotiated unit price override (e.g. for SFDC orders).
    /// Maps to: @item_json → $.unit_price (MONEY).
    /// </summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>
    /// Total item price override. Used for SFDC orders.
    /// Maps to: @item_json → $.item_total (MONEY).
    /// </summary>
    public decimal? ItemTotal { get; init; }

    /// <summary>
    /// Usage-based price (utility/autobilling overage).
    /// Maps to: @item_json → $.usage_price (MONEY).
    /// </summary>
    public decimal? UsagePrice { get; init; }

    /// <summary>
    /// Single vault identifier.
    /// Maps to: @item_json → $.vault_id (INT).
    /// TODO: REPLACE WITH ACTUAL — validate against configured vault list for this product/category.
    /// </summary>
    public int? VaultId { get; init; }

    /// <summary>
    /// Array of vault identifiers (multiple vault support).
    /// Maps to: @item_json → $.vault (NVARCHAR MAX, AS JSON).
    /// Serialized as a JSON array when passed to the SP.
    /// </summary>
    public List<int>? Vault { get; init; }

    /// <summary>
    /// Retention model identifier (e.g. 7 = "7 Years" for OTSF).
    /// Maps to: @item_json → $.retention_model_id (TINYINT).
    /// Note: retention_model_id=7 (OTSF 7-year) is blocked for partner cart orders.
    /// </summary>
    public byte? RetentionModelId { get; init; }

    /// <summary>
    /// Retention term in periods.
    /// Maps to: @item_json → $.retention_term (TINYINT).
    /// </summary>
    public byte? RetentionTerm { get; init; }

    /// <summary>
    /// Product platform (1=CEP, 3=On-Prem, etc.).
    /// Maps to: @item_json → $.product_platform_id (TINYINT).
    /// </summary>
    public byte? ProductPlatformId { get; init; }

    /// <summary>
    /// SAP material number for ERP integration.
    /// Maps to: @item_json → $.sap_material_number (INT).
    /// </summary>
    public int? SapMaterialNumber { get; init; }

    /// <summary>
    /// Salesforce amended contract ID. Skips unit override logic when set.
    /// Maps to: @item_json → $.amended_contract (VARCHAR 18).
    /// </summary>
    public string? AmendedContract { get; init; }

    // ── bundle_json fields (passed as a separate JSON blob to the SP) ─────────

    /// <summary>
    /// Keycode / message key for this specific line item (may differ from the order-level key).
    /// Maps to: @bundle_json → $.keycode (VARCHAR 40).
    /// </summary>
    public string? Keycode { get; init; }

    /// <summary>
    /// Controls how the SP handles an existing cart item with the same bundle ID.
    /// 1 = insert (default), other values = update modes.
    /// Maps to: @bundle_json → $.order_item_update_type_id (TINYINT).
    /// </summary>
    public byte? OrderItemUpdateTypeId { get; init; }

    /// <summary>
    /// Pre-applied discount from order-level context; can be overridden per item.
    /// Maps to: @bundle_json → $.cart_discount_id (INT).
    /// </summary>
    public int? CartDiscountId { get; init; }

    /// <summary>
    /// Pricing tier level for this item.
    /// Maps to: @bundle_json → $.product_pricing_level_id (TINYINT).
    /// </summary>
    public byte? ProductPricingLevelId { get; init; }

    /// <summary>
    /// Message key for this specific line item (may differ from the order-level key).
    /// Sent by the client; stored in cart_order_message when not null.
    /// </summary>
    public string? MessageKey { get; init; }
}

