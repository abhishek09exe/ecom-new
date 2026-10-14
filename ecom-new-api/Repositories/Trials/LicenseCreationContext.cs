using System.Text.Json.Serialization;

namespace ecom_new_api.Repositories.Trials;

public sealed record LicenseCreationContext(
    string PrimaryCategoryName,
    byte LicenseCategoryId,
    int ProductLineId,
    int LicenseTypeId,
    int LicenseKeycodeTypeId,
    bool IsTrial,
    int Seats,
    DateTime? StartDate,
    DateTime? ExpirationDate,
    int? LegacyTrialDays,
    int? CapabilityActivationDays,
    int TrialCapabilityTypeId,
    int LicenseDistributionMethodId,
    string LicenseDistributionMethodCode,
    IReadOnlyList<int> CapabilityIds,
    IReadOnlyList<LicenseCreationModule> Modules);

public sealed record LicenseCreationModule(
    string CategoryName,
    byte LicenseCategoryId,
    byte LicenseModuleId,
    string LicenseModuleCode,
    byte LicenseModuleTypeId,
    int Seats,
    DateTime? StartDate,
    DateTime? ExpirationDate);

internal sealed class LicenseCreationPayload
{
    [JsonPropertyName("license_keycode_type_id")]
    public int? KeycodeTypeId { get; init; }
    [JsonPropertyName("license_distribution_method_code")]
    public string? DistributionCode { get; init; }
    [JsonPropertyName("license_category")]
    public List<LicenseCreationCategory>? Categories { get; init; }
    [JsonPropertyName("parent_keycode")]
    public string? ParentKeycode { get; init; }
}

internal sealed class LicenseCreationCategory
{
    [JsonPropertyName("license_category_name")]
    public string? Name { get; init; }
    [JsonPropertyName("item_hierarchy_id")]
    public int HierarchyId { get; init; }
    [JsonPropertyName("category_type_name")]
    public string? CategoryType { get; init; }
    [JsonPropertyName("license_seats")]
    public int Seats { get; init; }
    [JsonPropertyName("days")]
    public int? Days { get; init; }
    [JsonPropertyName("start_date")]
    public DateTime? StartDate { get; init; }
    [JsonPropertyName("expiration_date")]
    public DateTime? ExpirationDate { get; init; }
}