using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Logging table for cart order item JSON payloads
/// Maps to [ecommerce_VH14].[dbo].[cart_order_item_json_log]
/// </summary>
[Table("cart_order_item_json_log")]
public class CartOrderItemJsonLogEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("cart_order_item_json_log_id")]
    public int CartOrderItemJsonLogId { get; set; }

    [Required]
    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("item_json")]
    public string? ItemJson { get; set; }

    [Column("bundle_json")]
    public string? BundleJson { get; set; }

    [Column("insert_date")]
    public DateTime? InsertDate { get; set; }
}
