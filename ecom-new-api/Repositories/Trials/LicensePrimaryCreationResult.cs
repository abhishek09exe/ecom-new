namespace ecom_new_api.Repositories.Trials;

public sealed record LicensePrimaryCreationResult(
    int LicenseCategoryLicenseId,
    IReadOnlyList<int> LicenseCapabilityIds,
    int LicenseSeatId);