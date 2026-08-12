namespace ecom_new_api.Models.Responses;

public sealed class CartOrderPartnerResponse
{
    public int PartnerId { get; init; }
    public string? PartnerKey { get; init; }
    public string? AccountUserName { get; init; }
}
