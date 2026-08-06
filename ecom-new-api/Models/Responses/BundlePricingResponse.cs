namespace ecom_new_api.Models.Responses;

public class BundlePricingResponse
{
    public List<PricingLineItem> Items { get; set; } = new();
    public PricingTotals Totals { get; set; } = new();
    public Dictionary<string, PricingTotals> ProductTotals { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
    public string CurrencySymbol { get; set; } = "$";
}

public class PricingLineItem
{
    public int LineItem { get; set; }
    public int Quantity { get; set; }
    public decimal ListPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UsagePrice { get; set; }
    public decimal EquivalentYearPrice { get; set; }
    public string? OrderItemOfferAmount { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public string ProductTypeDescription { get; set; } = string.Empty;
    public string LicenseCategoryName { get; set; } = string.Empty;
    public string LicenseCategoryDescription { get; set; } = string.Empty;
    public string? ProductFamilyDescription { get; set; }
    public string? StartDate { get; set; }
    public string? ExpirationDate { get; set; }
    public int CartItemBundleId { get; set; }
    public int ItemHierarchyId { get; set; }
    public int? LicenseKeycodeTypeId { get; set; }
    public int? DependentCartOrderItemId { get; set; }
    public int? StorageGb { get; set; }
    public int? UsagePricingModelId { get; set; }
    public int? RetentionModelId { get; set; }
    public string? RetentionTerm { get; set; }
    public string? RetentionModelName { get; set; }
    public string? ActualStorageQuantity { get; set; }
    public string? MessageKey { get; set; }
    public int? LicenseAttributeLicenseValue { get; set; }
    public int? CartDiscountId { get; set; }
    // Computed fields — not returned by SP
    public decimal CalculatedDiscount { get; set; }
    public decimal CalculatedDiscountPct { get; set; }
    public decimal SubTotalCalculatedDiscount { get; set; }
    public decimal SubTotalListAmount { get; set; }
    public decimal SubTotalAmount { get; set; }
    public decimal SubTotalEquivalentYearPrice { get; set; }
    public decimal EstimatedMonthlyPrice { get; set; }
    public string ListPriceFmt { get; set; } = string.Empty;
    public string UnitPriceFmt { get; set; } = string.Empty;
    public string UsagePriceFmt { get; set; } = string.Empty;
    public string EquivalentYearPriceFmt { get; set; } = string.Empty;
    public string CalculatedDiscountFmt { get; set; } = string.Empty;
    public string SubTotalCalculatedDiscountFmt { get; set; } = string.Empty;
    public string SubTotalListAmountFmt { get; set; } = string.Empty;
    public string SubTotalAmountFmt { get; set; } = string.Empty;
    public string SubTotalEquivalentYearPriceFmt { get; set; } = string.Empty;
    public string EstimatedMonthlyPriceFmt { get; set; } = string.Empty;
}

public class PricingTotals
{
    public decimal SubTotalEquivalentYearPrice { get; set; }
    public decimal SubTotalListAmount { get; set; }
    public decimal SubTotalAmount { get; set; }
    public decimal EstimatedMonthlyPrice { get; set; }
    public decimal SubTotalCalculatedDiscount { get; set; }
    public decimal CalculatedDiscountPct { get; set; }
    public string SubTotalEquivalentYearPriceFmt { get; set; } = string.Empty;
    public string SubTotalListAmountFmt { get; set; } = string.Empty;
    public string SubTotalAmountFmt { get; set; } = string.Empty;
    public string EstimatedMonthlyPriceFmt { get; set; } = string.Empty;
    public string SubTotalCalculatedDiscountFmt { get; set; } = string.Empty;
}
