using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order")]
public sealed class CartOrder
{
    [Key]
    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("vendor_order_code")]
    [MaxLength(100)]
    public string VendorOrderCode { get; set; } = default!;

    [Column("order_type")]
    [MaxLength(65)]
    public string? OrderType { get; set; }

    [Column("site_id")]
    [MaxLength(65)]
    public string SiteId { get; set; } = default!;

    [Column("site_url")]
    [MaxLength(65)]
    public string? SiteUrl { get; set; }

    [Column("offer_amount")]
    public decimal? OfferAmount { get; set; }

    [Column("total_amount")]
    public decimal? TotalAmount { get; set; }

    [Column("sub_total_amount")]
    public decimal? SubTotalAmount { get; set; }

    [Column("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [Column("sales_order_date")]
    public DateTime SalesOrderDate { get; set; }

    [Column("submission_date")]
    public DateTime? SubmissionDate { get; set; }

    [Column("locale")]
    [MaxLength(5)]
    public string Locale { get; set; } = default!;

    [Column("user_ip")]
    [MaxLength(16)]
    public string? UserIp { get; set; }

    [Column("currency_id")]
    public byte CurrencyId { get; set; }

    [Column("cart_order_status_id")]
    public int CartOrderStatusId { get; set; }

    [Column("insert_date")]
    public DateTime InsertDate { get; set; }

    [Column("insert_by")]
    [MaxLength(128)]
    public string? InsertBy { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }

    [Column("modified_by")]
    [MaxLength(128)]
    public string? ModifiedBy { get; set; }

    // Navigation
    public CartOrderPartner? CartOrderPartner { get; set; }
    public Currency? Currency { get; set; }
    public CartJson? CartJson { get; set; }
    public ICollection<CartOrderItem> Items { get; set; } = [];
    public CartOrderRoute? CartOrderRoute { get; set; }
    public CartOrderMessage? CartOrderMessage { get; set; }
}
