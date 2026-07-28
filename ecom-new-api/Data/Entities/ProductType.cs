namespace ecom_new_api.Data.Entities;

/// <summary>Maps to [dbo].[product_type].</summary>
public sealed class ProductType
{
    public int ProductTypeId { get; set; }
    public string? ProductTypeDescription { get; set; }
    public DateTime InsertDate { get; set; }
    public string InsertBy { get; set; } = default!;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = default!;
}
