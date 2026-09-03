namespace ecom_new_api.Repositories.Pricing;

/// <summary>
/// Typed input for a single pricing item passed to <see cref="IPricingRepository.GetItemPricingAsync"/>.
/// Replaces the JSON-serialised payloads previously sent to the configurator pricing SP.
/// </summary>
public sealed record BundleItemPricingInput(
    string  LicenseCategoryName,
    int     LicenseSeats,
    decimal Years,
    int     LicenseKeycodeTypeId,
    string  Locale,
    int     CartItemBundleId,
    byte    ItemHierarchyId,
    int?    StorageGb = null,
    int?    RetentionModelId = null
);
