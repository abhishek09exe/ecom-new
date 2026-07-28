using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_site_id_order_code_prefix")]
public sealed class CartSiteIdOrderCodePrefix
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("site_id")]
    [MaxLength(65)]
    public string SiteId { get; set; } = default!;

    [Column("vendor_order_code_prefix")]
    [MaxLength(5)]
    public string VendorOrderCodePrefix { get; set; } = default!;
}
