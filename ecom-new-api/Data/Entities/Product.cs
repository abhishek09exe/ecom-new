namespace ecom_new_api.Data.Entities;

public sealed class Product
{
    public int ProductId { get; set; }
    public string ProductDescription { get; set; } = default!; // NOT NULL
    public int ProductTypeId { get; set; }                     // NOT NULL
    public int? ProductFamilyId { get; set; }
    public int? ProductLifecycleId { get; set; }
    public int? LicenseKeycodeTypeId { get; set; }
    public int? RootProductId { get; set; }
    public int UsesKeycode { get; set; }                       // NOT NULL DEFAULT 0
    public int? CdProductId { get; set; }
    public decimal? RetailPrice { get; set; }
    public string? Basename { get; set; }
}
