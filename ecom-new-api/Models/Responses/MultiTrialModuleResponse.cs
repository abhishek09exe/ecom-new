namespace ecom_new_api.Models.Responses;

/// <summary>
/// A secondary product included in the Multi Trial registration (item_hierarchy_id = 2).
/// </summary>
public sealed class MultiTrialModuleResponse
{
    public string LicenseCategoryName { get; init; } = default!;

    public int LicenseSeats { get; init; }

    public int? VaultId { get; init; }
}
