using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_key")]
public sealed class LicenseKey
{
    [Key]
    [Column("license_id")]
    public int LicenseId { get; set; }

    // Column name is "license_key" matching the SP: WHERE license_key = @message_key
    [Column("license_key")]
    public Guid Key { get; set; }
}
