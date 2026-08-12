using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("item_hierarchy")]
public sealed class ItemHierarchy
{
    [Key]
    [Column("item_hierarchy_id")]
    public int ItemHierarchyId { get; set; }

    [Column("item_hierarchy_name")]
    [MaxLength(50)]
    public string ItemHierarchyName { get; set; } = default!;
}
