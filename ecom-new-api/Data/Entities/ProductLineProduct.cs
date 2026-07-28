namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [dbo].[product_line_product].
/// Join table — composite PK (product_id, product_line_id).
/// </summary>
public sealed class ProductLineProduct
{
    public int ProductLineId { get; set; }
    public int ProductId { get; set; }

    // Navigation properties
    public Product? Product { get; set; }
    public ProductLine? ProductLine { get; set; }
}
