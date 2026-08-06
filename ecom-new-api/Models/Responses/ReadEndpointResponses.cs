namespace ecom_new_api.Models.Responses;

public sealed class LicenseInfoResponse
{
    public string? Keycode { get; init; }
    public string? LicenseKey { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsExpired { get; init; }
    public int? LicenseAttributeLicenseValue { get; init; }
    public int? LicenseSeats { get; init; }
    public int? LicenseKeycodeTypeId { get; init; }
    public string? LicenseCategoryName { get; init; }
    public string? CapabilityTypeDescription { get; init; }
}

public sealed class LicenseProfileEntryResponse
{
    public string? LicenseCategoryName { get; init; }
    public string? LicenseCategoryDescription { get; init; }
    public string? CategoryTypeName { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public int? LicenseSeats { get; init; }
}

public sealed class LicenseOptionsResponse
{
    public Dictionary<string, object?> License { get; init; } = [];
    public bool LicenseVerified { get; init; }
    public Dictionary<string, Dictionary<string, object?>> LicenseProfile { get; init; } = [];
    public object? LicenseSiteId { get; init; }
    public Dictionary<string, Dictionary<string, object?>> UpgradeCategories { get; init; } = [];
    public Dictionary<string, Dictionary<string, object?>> BillingModels { get; init; } = [];
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
