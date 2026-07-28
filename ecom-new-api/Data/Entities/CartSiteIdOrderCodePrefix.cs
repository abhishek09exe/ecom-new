namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [cart_site_id_order_code_prefix].
/// Stores the short prefix string used to build vendor_order_code per site_id.
/// SP section 2.1: SELECT vendor_order_code_prefix FROM cart_site_id_order_code_prefix WHERE site_id = @site_id
/// </summary>
public sealed class CartSiteIdOrderCodePrefix
{
    /// <summary>PK — identity int.</summary>
    public int CartSiteIdOrderCodePrefixId { get; set; }

    /// <summary>site_id value passed on the cart order request (unique, not null).</summary>
    public string SiteId { get; set; } = string.Empty;

    /// <summary>Short prefix prepended to the sequential number, e.g. "ECM", "GSM".</summary>
    public string VendorOrderCodePrefix { get; set; } = string.Empty;

    /// <summary>Optional human-readable description of this site mapping.</summary>
    public string? SiteIdDescription { get; set; }
}
