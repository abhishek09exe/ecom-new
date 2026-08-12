using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to the <c>ids</c> table — holds monotonically incrementing sequences
/// used in place of DB IDENTITY columns for several entity types.
/// <para>
/// Relevant rows:
///   id_type = 3 → cart order vendor_order_code sequence (Invoices)
/// </para>
/// </summary>
[Table("ids")]
public sealed class IdsRow
{
    [Key]
    [Column("id_type")]
    public int IdType { get; set; }

    [Column("next_id")]
    public int NextId { get; set; }

    [Column("description")]
    [MaxLength(32)]
    public string Description { get; set; } = string.Empty;

    [Column("last_modified")]
    public DateTime LastModified { get; set; }
}
