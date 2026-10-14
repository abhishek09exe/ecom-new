using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_capability")]
public sealed class ProductCapability
{
    [Key]
    [Column("product_capability_id")]
    public int ProductCapabilityId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("capability_id")]
    public int CapabilityId { get; set; }

    [Column("capability_type_id")]
    public int CapabilityTypeId { get; set; }

    [Column("capability_activation_days")]
    public int? CapabilityActivationDays { get; set; }

    public Product? Product { get; set; }
}