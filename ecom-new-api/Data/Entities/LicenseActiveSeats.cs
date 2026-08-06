using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Data.Entities;

[Keyless]
[Table("license_active_seats")]
public sealed class LicenseActiveSeats
{
    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("consumed_seats")]
    public int ConsumedSeats { get; set; }
}
