using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a cart order (shopping cart header)
/// Maps to [ecommerce_VH14].[dbo].[cart_order]
/// </summary>
[Table("cart_order")]
public class CartOrderEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("vendor_order_code")]
    public string VendorOrderCode { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [Column("site_id")]
    public string SiteId { get; set; } = null!;

    [MaxLength(100)]
    [Column("site_url")]
    public string? SiteUrl { get; set; }

    [MaxLength(50)]
    [Column("order_type")]
    public string? OrderType { get; set; }

    [Column("offer_amount", TypeName = "money")]
    public decimal? OfferAmount { get; set; }

    [Column("total_amount", TypeName = "money")]
    public decimal? TotalAmount { get; set; }

    [Column("sub_total_amount", TypeName = "money")]
    public decimal? SubTotalAmount { get; set; }

    [Column("tax_amount", TypeName = "money")]
    public decimal? TaxAmount { get; set; }

    [Required]
    [Column("sales_order_date")]
    public DateTime SalesOrderDate { get; set; }

    [Column("submission_date")]
    public DateTime? SubmissionDate { get; set; }

    [Required]
    [MaxLength(5)]
    [Column("locale")]
    public string Locale { get; set; } = null!;

    [Required]
    [Column("insert_date")]
    public DateTime InsertDate { get; set; }

    [MaxLength(256)]
    [Column("insert_by")]
    public string? InsertBy { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }

    [MaxLength(256)]
    [Column("modified_by")]
    public string? ModifiedBy { get; set; }

    [Column("cart_order_status_id")]
    public int? CartOrderStatusId { get; set; }

    [MaxLength(45)]
    [Column("user_ip")]
    public string? UserIp { get; set; }

    [Column("currency_id")]
    [ForeignKey("Currency")]
    public int CurrencyId { get; set; }

    // Navigation properties
    public virtual CurrencyEntity Currency { get; set; } = null!;
    public virtual ICollection<CartOrderItemEntity> CartOrderItems { get; set; } = [];
    public virtual ICollection<CartOrderPartnerEntity> CartOrderPartners { get; set; } = [];
    public virtual CartOrderJsonEntity? CartJson { get; set; }
}
