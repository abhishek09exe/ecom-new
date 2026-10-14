using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Data.Entities;

[Table("license")]
public sealed class License
{
    [Key]
    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("keycode")]
    [MaxLength(40)]
    [Unicode(false)]
    public string Keycode { get; set; } = default!;

    [Column("customer_id")]
    public int? CustomerId { get; set; }

    [Column("product_line_id")]
    public int ProductLineId { get; set; }

    [Column("license_status_id")]
    public int LicenseStatusId { get; set; }

    [Column("license_type_id")]
    public int LicenseTypeId { get; set; }

    [Column("license_distribution_method_id")]
    public int? LicenseDistributionMethodId { get; set; }

    [Column("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }

    [Column("max_daily_activations")]
    public int MaxDailyActivations { get; set; }

    [Column("license_expiration_date", TypeName = "datetime")]
    public DateTime? LicenseExpirationDate { get; set; }

    [Column("insert_date", TypeName = "datetime")]
    public DateTime InsertDate { get; set; }

    [Column("insert_by"), MaxLength(50), Unicode(false)]
    public string InsertBy { get; set; } = string.Empty;

    [Column("modified_date", TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [Column("modified_by"), MaxLength(50), Unicode(false)]
    public string ModifiedBy { get; set; } = string.Empty;
}
