namespace ecom_new_api.Data.Entities;

/// <summary>Maps to [dbo].[product_line].</summary>
public sealed class ProductLine
{
    public int ProductLineId { get; set; }
    public string ProductLineDescription { get; set; } = default!;
    public string ProductLinePrefix { get; set; } = default!;
    public int RootProductId { get; set; }
    public DateTime InsertDate { get; set; }
    public string InsertBy { get; set; } = default!;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = default!;
    public byte? Status { get; set; }
    public string? ProductLineCartType { get; set; }
}
