using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("capability_type")]
public sealed class CapabilityType
{
    [Key]
    [Column("capability_type_id")]
    public int CapabilityTypeId { get; set; }

    [Column("capability_type_description")]
    [MaxLength(20)]
    public string CapabilityTypeDescription { get; set; } = default!;
}
