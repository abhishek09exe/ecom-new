namespace ecom_new_api.Data.Entities;

/// <summary>Maps to [dbo].[license_keycode_type].</summary>
public sealed class LicenseKeycodeType
{
    public int LicenseKeycodeTypeId { get; set; }
    public string LicenseKeycodeTypeDescription { get; set; } = default!;
}
