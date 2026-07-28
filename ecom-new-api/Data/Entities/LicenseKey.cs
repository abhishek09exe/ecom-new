namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [license_key].
/// SP usp_cart_insert_cart_order section 2.5:
///   SELECT license_id FROM license_key WHERE license_key = @message_key
/// Read-only — never written from this service.
/// </summary>
public sealed class LicenseKey
{
    public int LicenseKeyId { get; set; }

    /// <summary>The keycode GUID — matches the request.Key field (message_key in SP).</summary>
    public Guid LicenseKeyValue { get; set; }

    public int LicenseId { get; set; }
    public string? SalesforceLicenseId { get; set; }
}
