namespace ecom_new_api.Models.Responses;

/// <summary>
/// Response shape for GET /license-options.
/// TODO: REPLACE WITH ACTUAL — flesh out properties once usp_cart_select_message_key
/// and usp_license_select_license_by_id result sets are mapped.
/// </summary>
public sealed class LicenseOptionsResponse
{
    public string Keycode { get; init; } = default!;
    public string? LicenseStatus { get; init; }
    public string? LicenseCategory { get; init; }
    public int? LicenseSeats { get; init; }
    public DateTime? ExpirationDate { get; init; }

    /// <summary>Available product options for this license (TRIAL / RENEW / ADD SEATS tabs).</summary>
    public List<ProductOptionResponse> ProductOptions { get; init; } = [];
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

/// <summary>
/// Represents a single selectable product in a license-options / configure / upgrade response.
/// TODO: REPLACE WITH ACTUAL — map full column list from the stored procedure result sets.
/// </summary>
public sealed class ProductOptionResponse
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string? LicenseCategoryName { get; init; }
    public decimal? Price { get; init; }
    public int? Years { get; init; }
    public int? Seats { get; init; }
}
