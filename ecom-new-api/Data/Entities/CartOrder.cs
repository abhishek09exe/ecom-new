using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order")]
public class CartOrder
{
    [Key]
    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("cart_customer_id")]
    public int CartCustomerId { get; set; }

    [Column("invoice_in_process_id")]
    public int InvoiceInProcessId { get; set; }

    [Column("vendor_order_code")]
    public string? VendorOrderCode { get; set; }

    [Column("order_type")]
    public string OrderType { get; set; } = string.Empty;

    [Column("site_id")]
    public string SiteId { get; set; } = string.Empty;

    [Column("site_url")]
    public string SiteUrl { get; set; } = string.Empty;

    [Column("p_rc")]
    public string PRc { get; set; } = string.Empty;

    [Column("p_rsc")]
    public string? PRsc { get; set; }

    [Column("p_ac")]
    public string? PAc { get; set; }

    [Column("trx_rc")]
    public string? TrxRc { get; set; }

    [Column("trx_rsc")]
    public string? TrxRsc { get; set; }

    [Column("trx_ac")]
    public string? TrxAc { get; set; }

    [Column("aid")]
    public string? Aid { get; set; }

    [Column("pid")]
    public string? Pid { get; set; }

    [Column("sid")]
    public string? Sid { get; set; }

    [Column("offer_id")]
    public string? OfferId { get; set; }

    [Column("offer_amount")]
    public decimal? OfferAmount { get; set; }

    [Column("total_amount")]
    public decimal? TotalAmount { get; set; }

    [Column("sub_total_amount")]
    public decimal SubTotalAmount { get; set; }

    [Column("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("exchange_rate")]
    public decimal? ExchangeRate { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("submission_date")]
    public DateTime SubmissionDate { get; set; }

    [Column("sales_order_date")]
    public DateTime? SalesOrderDate { get; set; }

    [Column("locale")]
    public string Locale { get; set; } = string.Empty;

    [Column("subject")]
    public string? Subject { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("insert_date")]
    public DateTime InsertDate { get; set; }

    [Column("insert_by")]
    public string InsertBy { get; set; } = string.Empty;

    [Column("modified_date")]
    public DateTime ModifiedDate { get; set; }

    [Column("modified_by")]
    public string ModifiedBy { get; set; } = string.Empty;

    [Column("cart_order_status_id")]
    public byte CartOrderStatusId { get; set; }

    [Column("currency_id")]
    public byte? CurrencyId { get; set; }

    [Column("customer_profile_token")]
    public string? CustomerProfileToken { get; set; }

    [Column("cart_order_in_process_id")]
    public int? CartOrderInProcessId { get; set; }

    [Column("user_ip")]
    public string? UserIp { get; set; }

    [Column("restriction")]
    public string? Restriction { get; set; }

    // Navigation Properties
    public Currency? Currency { get; set; }

    public CartJson? CartJson { get; set; }

    public ICollection<CartOrderPartner> CartOrderPartners { get; set; }
        = new List<CartOrderPartner>();
}