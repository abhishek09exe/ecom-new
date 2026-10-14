namespace ecom_new_api.Repositories.Trials;

public sealed record LicenseCreationResult(
    int LicenseCategoryLicenseId,
    IReadOnlyList<int> LicenseCapabilityIds,
    int LicenseSeatId,
    IReadOnlyList<int> LicenseModuleLicenseIds,
    Guid LicenseKey,
    int? LicenseFeatureLicenseId);