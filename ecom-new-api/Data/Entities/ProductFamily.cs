using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_family")]
public sealed class ProductFamily
{
    [Key]
    [Column("product_family_id")]
    public int ProductFamilyId { get; set; }

    [Column("product_family_description")]
    [MaxLength(255)]
    public string? ProductFamilyDescription { get; set; }
}
