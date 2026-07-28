namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [account].
/// SP usp_cart_insert_cart_order section 2.3:
///   JOIN account a ON p.account_id = a.account_id
///   WHERE a.account_user_name = @account_user_name
/// Only the columns needed for the partner_account_id lookup are mapped here.
/// </summary>
public sealed class Account
{
    public int AccountId { get; set; }
    public string? AccountUserName { get; set; }
}
