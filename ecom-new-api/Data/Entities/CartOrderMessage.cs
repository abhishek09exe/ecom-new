namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [cart_order_message].
/// SP usp_cart_insert_cart_order section 2.5:
///   INSERT INTO cart_order_message (cart_order_id, message_key, message_campaign_id,
///     message_campaign_platform, cart_discount_id, license_id)
/// Also used by quote-key pivot (G14): query by message_key to find existing vendor_order_code.
/// </summary>
public sealed class CartOrderMessage
{
    public int CartOrderMessageId { get; set; }
    public int CartOrderId { get; set; }
    public Guid MessageKey { get; set; }
    public int? LicenseId { get; set; }
    public int? CartDiscountId { get; set; }
    public byte StatusId { get; set; }
    public int? MessageCampaignId { get; set; }
    public string? MessageCampaignPlatform { get; set; }

    public CartOrder? CartOrder { get; set; }
}
