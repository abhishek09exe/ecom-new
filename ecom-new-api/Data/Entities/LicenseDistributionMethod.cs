using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_distribution_method")]
public sealed class LicenseDistributionMethod
{
    [Key]
    [Column("license_distribution_method_id")]
    public int LicenseDistributionMethodId { get; set; }

    [Column("license_distribution_method_code")]
    [MaxLength(4)]
    public string LicenseDistributionMethodCode { get; set; } = default!;
}
