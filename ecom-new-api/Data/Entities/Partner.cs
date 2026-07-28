namespace ecom_new_api.Data.Entities;

public sealed class Partner
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = default!; // NOT NULL
    public byte PartnerTypeId { get; set; }             // NOT NULL
    public byte PartnerStatusId { get; set; }           // NOT NULL DEFAULT 1
    public Guid PartnerKey { get; set; }                // NOT NULL DEFAULT newid()
    public int? ParentPartnerId { get; set; }
    public string? SalesforceId { get; set; }
    public string? OracleId { get; set; }
    public string? AccountOwnerId { get; set; }

    // Navigation
    public ICollection<CartOrderPartner> CartOrderPartners { get; set; } = [];
}
