using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_line")]
public sealed class ProductLine
{
    [Key]
    [Column("product_line_id")]
    public int ProductLineId { get; set; }

    [Column("product_line_cart_type")]
    [MaxLength(50)]
    public string? ProductLineCartType { get; set; }
}
