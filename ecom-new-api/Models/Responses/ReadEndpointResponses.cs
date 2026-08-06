namespace ecom_new_api.Models.Responses;

using System.Text.Json.Serialization;

public sealed class LicenseInfoResponse
{
    public string? Keycode { get; init; }
    public string? ProductLineDescription { get; init; }
    public int? LicenseStatusId { get; init; }
    public string? LicenseTypeDescription { get; init; }
    public int? LicenseKeycodeTypeId { get; init; }
    public int? MaxDailyActivations { get; init; }
    public DateTime? LicenseExpirationDate { get; init; }
    public string? ParentKeycode { get; init; }
    public string? LicenseKey { get; init; }
    public int? LicenseSeats { get; init; }
    public int? ConsumedSeats { get; init; }
    public int? SeatsUsed { get; init; }
    public int? StorageGb { get; init; }
    public string? LicenseCategoryName { get; init; }
    public string? LicenseCategoryDescription { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? DaysRemaining { get; init; }
    public bool IsExpired { get; init; }
    public string? LicenseAttributeDescription { get; init; }
    public string? LicenseAttributeTag { get; init; }
    public int? LicenseAttributeLicenseValue { get; init; }
    public string? LicenseAttributeLicenseValueDescription { get; init; }
    public DateTime? LicenseAttributeLastModified { get; init; }
    public string? OemType { get; init; }
    public int? PortalFlag { get; init; }
    public int? RenewalCount { get; init; }
    public string? LicenseOriginChannelName { get; init; }
    public DateTime? LicenseOriginalActivationDate { get; init; }
    public int? EmailOptIn { get; init; }
    public string? LicenseDistributionMethodCode { get; init; }
    public DateTime? NextBillDate { get; init; }
    public string? CapabilityTypeDescription { get; init; }
}

public sealed class LicenseProfileEntryResponse
{
    public string? LicenseCategoryName { get; init; }
    public string? LicenseCategoryDescription { get; init; }
    public int? LicenseCategoryId { get; init; }
    public int? LicenseKeycodeTypeId { get; init; }
    public string? CategoryTypeName { get; init; }
    public int? LicenseStatusId { get; init; }
    public string? LicenseStatusDescription { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public int? LicenseSeats { get; init; }
    public int? StorageGb { get; init; }
    public int? LicenseAttributeId { get; init; }
    public string? LicenseAttributeDescription { get; init; }
    public int? LicenseAttributeLicenseValue { get; init; }
    public string? LicenseAttributeLicenseValueDescription { get; init; }
    public int? ItemHierarchyId { get; init; }
    public string? ItemHierarchyName { get; init; }
    public string? AutorenewalCycleName { get; init; }
    public decimal? AutorenewalCycle { get; init; }
    public int? UsagePricingModelId { get; init; }
    public string? UsagePricingModelName { get; init; }
    public int? RetentionModelId { get; init; }
    public string? RetentionModelName { get; init; }
    public int? RetentionTerm { get; init; }
    public int? RetentionModelTypeId { get; init; }
    public int? ProductPlatformId { get; init; }
    public string? ProductPlatformName { get; init; }
    public int? LicenseAutorenewalValue { get; init; }
    public int? ProductPricingLevelId { get; init; }
    public string? PricingLevel { get; init; }
    public string? PricingLevelDescription { get; init; }
    public string? LicenseVaultJson { get; init; }
    public double? MostRecentOrderTerm { get; init; }
}

public sealed class UpgradeCategoryResponse
{
    public string? LicenseCategoryName { get; init; }
    public string? UpgradeLicenseCategoryName { get; init; }
    public int? ItemHierarchyId { get; init; }
    public string? ItemHierarchyName { get; init; }
}

public sealed class BillingModelResponse
{
    public int? ProductTypeId { get; init; }
    public string? ProductTypeDescription { get; init; }
    public string? LicenseAttributeDescription { get; init; }
    public int? LicenseAttributeLicenseValue { get; init; }
    public string? LicenseAttributeLicenseValueDescription { get; init; }
}

public sealed class LicenseOptionsResponse
{
    public string Keycode { get; init; } = default!;
    public string? LicenseKey { get; init; }
    public string? LicenseStatus { get; init; }
    public string? ProductLine { get; init; }
    public string? LicenseCategory { get; init; }
    public string? LicenseCategoryDescription { get; init; }
    public int? LicenseSeats { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public List<ProductOptionResponse> ProductOptions { get; init; } = [];
    public LicenseInfoResponse? License { get; init; }
    [JsonPropertyName("license_verified")]
    public bool LicenseVerified { get; init; }
    public Dictionary<string, LicenseProfileEntryResponse> LicenseProfile { get; init; } = [];
    public string? LicenseSiteId { get; init; }
    public Dictionary<string, UpgradeCategoryResponse> UpgradeCategories { get; init; } = [];
    public List<BillingModelResponse> BillingModels { get; init; } = [];
}

/// <summary>
/// Response shape for GET /configure.
/// TODO: REPLACE WITH ACTUAL — flesh out from usp_partner_cart_select_order_page_details result set.
/// </summary>
public sealed class ConfigureResponse
{
    public string Keycode { get; init; } = default!;
    public List<ProductOptionResponse> RenewalOptions { get; init; } = [];
}

/// <summary>
/// Response shape for GET /upgrade.
/// TODO: REPLACE WITH ACTUAL — flesh out from usp_product_select_license_category_upgrade result set.
/// </summary>
public sealed class UpgradeResponse
{
    public string Keycode { get; init; } = default!;
    public List<ProductOptionResponse> UpgradeOptions { get; init; } = [];
}

public sealed class ProductOptionResponse
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string? LicenseCategoryName { get; init; }
    public string? ProductTypeDescription { get; init; }
    public decimal? Price { get; init; }
    public List<double> Years { get; init; } = [];
    public List<int> Seats { get; init; } = [];
}
