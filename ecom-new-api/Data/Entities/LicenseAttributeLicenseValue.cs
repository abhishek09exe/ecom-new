namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [dbo].[license_attribute_license_value].
/// Lookup table for billing model descriptions.
/// Note: the PK column shares the same name as the FK on cart_order_item.
/// </summary>
public sealed class LicenseAttributeLicenseValue
{
    /// <summary>PK — same int value stored in cart_order_item.license_attribute_license_value.</summary>
    public int LicenseAttributeLicenseValueId { get; set; }

    public string LicenseAttributeLicenseValueDescription { get; set; } = default!;
}
