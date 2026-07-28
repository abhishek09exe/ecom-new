namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to the cart_order_partner table.
/// Join table that links a cart_order to a partner.
/// </summary>
public sealed class CartOrderPartner
{
    public int CartOrderPartnerId { get; set; }
    public int CartOrderId { get; set; }
    public int PartnerId { get; set; }
    public int? PartnerAccountId { get; set; }

    // Navigation
    public CartOrder CartOrder { get; set; } = default!;
    public Partner Partner { get; set; } = default!;
}
