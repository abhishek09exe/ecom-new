using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("order_item_license")]
public sealed class OrderItemLicense
{
    [Key]
    [Column("order_item_license_id")]
    public int OrderItemLicenseId { get; set; }

    [Column("order_item_id")]
    public int OrderItemId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }
}
