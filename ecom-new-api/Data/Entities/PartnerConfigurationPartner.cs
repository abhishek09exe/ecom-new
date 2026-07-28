namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [partner_configuration_partner].
/// SP usp_cart_insert_cart_order section 1.3.2:
///   SELECT currency_code, currency_id FROM partner_configuration_partner
///   INNER JOIN currency ON cp.configuration_value = c.currency_code
///   WHERE cp.partner_id = @partner_id AND cp.partner_configuration_id = 15
/// Read-only — used for partner currency fallback when no currency is in the request.
/// </summary>
public sealed class PartnerConfigurationPartner
{
    public int PartnerConfigurationPartnerId { get; set; }
    public int PartnerId { get; set; }
    public byte PartnerConfigurationId { get; set; }

    /// <summary>For configuration_id=15 this holds the currency_code (e.g. "USD", "EUR").</summary>
    public string ConfigurationValue { get; set; } = string.Empty;
}
