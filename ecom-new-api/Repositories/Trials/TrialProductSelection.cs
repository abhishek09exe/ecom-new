namespace ecom_new_api.Repositories.Trials;

public sealed record TrialProductSelection
{
    public int ProductId { get; init; }
    public string? ProductDescription { get; init; }
    public int ProductTypeId { get; init; }
    public string? LicenseCategoryName { get; init; }
    public byte LicenseCategoryId { get; init; }
    public int ProductLineId { get; init; }
    public string? LanguageCode { get; init; }
    public string? LocationCode { get; init; }
    public double Years { get; init; }
    public int CapabilityId { get; init; }
    public int CapabilityTypeId { get; init; }
    public int? CapabilityActivationDays { get; init; }
    public decimal? RetailPrice { get; init; }
    public decimal? EducationNonProfitPrice { get; init; }
    public decimal? GovernmentPrice { get; init; }
    public int? CurrencyId { get; init; }
    public string? CurrencyCode { get; init; }
    public string? CurrencyDescription { get; init; }
    public string? SymbolHtml { get; init; }
    public string? SymbolUtf8 { get; init; }
    public string? SymbolText { get; init; }
    public decimal? StandardDiscountPerItem { get; init; }
    public decimal? SupplementalDiscountPerItem { get; init; }
    public int? AppliedTierDiscountId { get; init; }
    public int? AppliedCampaignDiscountId { get; init; }
    public decimal? ExtendedRetailPrice { get; init; }
    public decimal? ExtendedStandardDiscount { get; init; }
    public decimal? ExtendedSupplementalDiscount { get; init; }
    public decimal? NetPrice { get; init; }
    public decimal? UpgradePrice { get; init; }
    public int? LegacyCartDiscountId { get; init; }
}