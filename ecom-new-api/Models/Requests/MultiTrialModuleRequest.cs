using System.Text.Json.Serialization;

namespace ecom_new_api.Models.Requests;

/// <summary>
/// A secondary product selected on the Multi Trial form.
/// Serialised into trial_registration_json.license_category with item_hierarchy_id = 2.
/// </summary>
public sealed class MultiTrialModuleRequest
{
    /// <summary>Module product code. Legacy alias: license_module_code.</summary>
    public string? LicenseCategoryName { get; init; }

    /// <summary>Seat count for the module. Legacy alias: license_module_seats.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int LicenseSeats { get; init; }

    /// <summary>Optional data-centre vault value passed through with the module.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? VaultId { get; init; }
}
