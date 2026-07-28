namespace ecom_new_api.Data.Entities;

/// <summary>Maps to [dbo].[product_family].</summary>
public sealed class ProductFamily
{
    public int ProductFamilyId { get; set; }
    public string ProductFamilyDescription { get; set; } = default!;
    public string? ProductFamilyPrefix { get; set; }
    public DateTime InsertDate { get; set; }
    public string InsertBy { get; set; } = default!;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = default!;
}
