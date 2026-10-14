using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_feature_license")]
public sealed class LicenseFeatureLicense
{
    [Key, Column("license_feature_license_id")]
    public int LicenseFeatureLicenseId { get; set; }
    [Column("license_id")]
    public int LicenseId { get; set; }
    [Column("license_feature_id")]
    public int LicenseFeatureId { get; set; }
    [Column("insert_date", TypeName = "datetime")]
    public DateTime InsertDate { get; set; }
    [Column("insert_by"), MaxLength(200)]
    public string InsertBy { get; set; } = string.Empty;
    [Column("license_feature_value"), MaxLength(20)]
    public string? LicenseFeatureValue { get; set; }
}