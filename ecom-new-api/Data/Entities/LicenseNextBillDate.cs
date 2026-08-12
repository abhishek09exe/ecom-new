using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_next_bill_date")]
public sealed class LicenseNextBillDate
{
    [Key]
    [Column("license_next_bill_date_id")]
    public int LicenseNextBillDateId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("next_bill_date")]
    public DateTime NextBillDate { get; set; }
}
