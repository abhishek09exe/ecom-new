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

    [Column("cart_customer_id")]
    public int CartCustomerId { get; set; } = 0;  // sentinel: no customer yet at cart creation

    [Column("invoice_in_process_id")]
    public int InvoiceInProcessId { get; set; } = 0;  // sentinel: payment workflow, not cart creation

    [Column("order_type")]
    [MaxLength(30)]
    public string OrderType { get; set; } = default!;

    [Column("site_id")]
    [MaxLength(65)]
    public string SiteId { get; set; } = default!;

    [Column("site_url")]
    [MaxLength(1025)]
    public string SiteUrl { get; set; } = default!;

    [Column("p_rc")]
    [MaxLength(50)]
    public string PRc { get; set; } = string.Empty;

    [Column("payment_method")]
    [MaxLength(255)]
    public string PaymentMethod { get; set; } = "PENDING";

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("offer_amount")]
    public decimal? OfferAmount { get; set; }

    [Column("total_amount")]
    public decimal? TotalAmount { get; set; }

    [Column("sub_total_amount")]
    public decimal SubTotalAmount { get; set; }

    [Column("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [Column("sales_order_date")]
    public DateTime SalesOrderDate { get; set; }

    [Column("submission_date")]
    public DateTime SubmissionDate { get; set; }

    [Column("locale")]
    [MaxLength(5)]
    public string Locale { get; set; } = default!;

    [Column("user_ip")]
    [MaxLength(16)]
    public string? UserIp { get; set; }

    [Column("currency_id")]
    public byte CurrencyId { get; set; }

    [Column("cart_order_status_id")]
    public byte CartOrderStatusId { get; set; }

    [Column("insert_date")]
    public DateTime InsertDate { get; set; }

    [Column("insert_by")]
    [MaxLength(50)]
    public string InsertBy { get; set; } = default!;

    [Column("modified_date")]
    public DateTime ModifiedDate { get; set; }

    [Column("modified_by")]
    [MaxLength(50)]
    public string ModifiedBy { get; set; } = default!;

    // Navigation
    public CartOrderPartner? CartOrderPartner { get; set; }
    public Currency? Currency { get; set; }
    public CartJson? CartJson { get; set; }
    public ICollection<CartOrderItem> Items { get; set; } = [];
    public CartOrderRoute? CartOrderRoute { get; set; }
    public CartOrderMessage? CartOrderMessage { get; set; }
}
