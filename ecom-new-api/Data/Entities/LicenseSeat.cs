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

    [Column("start_date", TypeName = "datetime")]
    public DateTime? StartDate { get; set; }
    [Column("end_date", TypeName = "datetime")]
    public DateTime? EndDate { get; set; }
    [Column("insert_date", TypeName = "datetime")]
    public DateTime InsertDate { get; set; }
    [Column("insert_by"), MaxLength(50)]
    public string InsertBy { get; set; } = string.Empty;
    [Column("modified_date", TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }
    [Column("modified_by"), MaxLength(50)]
    public string ModifiedBy { get; set; } = string.Empty;
}
