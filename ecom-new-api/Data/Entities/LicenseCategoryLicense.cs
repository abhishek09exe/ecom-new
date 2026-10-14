using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_category_license")]
public sealed class LicenseCategoryLicense
{
    [Key]
    [Column("license_category_license_id")]
    public int LicenseCategoryLicenseId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    // tinyint in DB — cast to int at join site in the repository
    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }

    [Column("start_date", TypeName = "datetime")]
    public DateTime? StartDate { get; set; }

    [Column("end_date", TypeName = "datetime")]
    public DateTime? EndDate { get; set; }

    [Column("last_modified_date", TypeName = "datetime")]
    public DateTime LastModifiedDate { get; set; }

    [Column("last_modified_by"), MaxLength(200)]
    public string? LastModifiedBy { get; set; }
}
