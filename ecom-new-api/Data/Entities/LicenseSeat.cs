using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_seat")]
public sealed class LicenseSeat
{
    [Key]
    [Column("license_seat_id")]
    public int LicenseSeatId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("license_seats")]
    public int LicenseSeats { get; set; }

    [Column("seats_used")]
    public int SeatsUsed { get; set; }
}
