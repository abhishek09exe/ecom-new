namespace ecom_new_api.Models.Requests;

/// <summary>
/// A secondary product selected on the Multi Trial form (SDNS, SECA, OTEDR, OTMDR, …).
/// Serialised into trial_registration_json.license_category with item_hierarchy_id = 2.
/// </summary>
public sealed class MultiTrialModuleRequest
{
    /// <summary>Module product code. Legacy alias: license_module_code.</summary>
    public string? LicenseCategoryName { get; init; }

    /// <summary>Seat count for the module. Legacy alias: license_module_seats.</summary>
    public int LicenseSeats { get; init; }

    /// <summary>Data-centre vault, required by EDR/MDR modules.</summary>
    public int? VaultId { get; init; }
}
