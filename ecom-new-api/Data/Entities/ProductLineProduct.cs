using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_line_product")]
public sealed class ProductLineProduct
{
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("product_line_id")]
    public int ProductLineId { get; set; }

    // Navigation
    public Product? Product { get; set; }
    public ProductLine? ProductLine { get; set; }
}
