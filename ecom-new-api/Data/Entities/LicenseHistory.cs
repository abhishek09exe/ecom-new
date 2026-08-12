using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_history")]
public sealed class LicenseHistory
{
    [Key]
    [Column("license_history_id")]
    public int LicenseHistoryId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("license_distribution_method_id")]
    public int LicenseDistributionMethodId { get; set; }

    [Column("insert_date")]
    public DateTime InsertDate { get; set; }

    [Column("history_date")]
    public DateTime HistoryDate { get; set; }
}
