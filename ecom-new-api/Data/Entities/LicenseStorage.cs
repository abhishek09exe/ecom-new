using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_storage")]
public sealed class LicenseStorage
{
    [Key]
    [Column("license_storage_id")]
    public int LicenseStorageId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("storage_gb")]
    public int StorageGb { get; set; }
}
