using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_key")]
public sealed class LicenseKey
{
    [Key]
    [Column("license_id")]
    public int LicenseId { get; set; }

    // uniqueidentifier in DB — must be Guid to avoid SqlDataReader.GetString() cast failure on reads
    [Column("license_key")]
    public Guid Key { get; set; }
}
