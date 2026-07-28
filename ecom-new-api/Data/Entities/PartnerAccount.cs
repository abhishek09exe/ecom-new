namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [partner_account].
/// SP usp_cart_insert_cart_order section 2.3:
///   SELECT partner_account_id FROM partner_account JOIN account
///   WHERE partner_id = @partner_id AND account_user_name = @account_user_name
/// Read-only — used to resolve partner_account_id when inserting cart_order_partner.
/// </summary>
public sealed class PartnerAccount
{
    public int PartnerAccountId { get; set; }
    public int PartnerId { get; set; }
    public int AccountId { get; set; }
}
