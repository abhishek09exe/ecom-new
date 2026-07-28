using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_type")]
public sealed class ProductType
{
    [Key]
    [Column("product_type_id")]
    public int ProductTypeId { get; set; }

    [Column("product_type_description")]
    [MaxLength(255)]
    public string? ProductTypeDescription { get; set; }
}
