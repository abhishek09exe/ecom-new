using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_parent")]
public sealed class LicenseParent
{
    [Key]
    [Column("license_parent_id")]
    public int LicenseParentId { get; set; }

    [Column("parent_license_id")]
    public int ParentLicenseId { get; set; }

    [Column("child_license_id")]
    public int ChildLicenseId { get; set; }
}
