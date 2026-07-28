namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to the cart_order table.
/// Navigation properties replace the JOINs the old stored procedures did manually.
/// </summary>
public sealed class CartOrder
{
    public int CartOrderId { get; set; }
    public int CartCustomerId { get; set; }          // NOT NULL DEFAULT 0
    public int InvoiceInProcessId { get; set; }      // NOT NULL DEFAULT 0
    public string? VendorOrderCode { get; set; }
    public string OrderType { get; set; } = "cart";
    public string SiteId { get; set; } = default!;
    public string SiteUrl { get; set; } = string.Empty;
    public string PRc { get; set; } = "1";           // NOT NULL DEFAULT '1'
    public string? PRsc { get; set; }
    public string? PAc { get; set; }
    public string? TrxRc { get; set; }
    public string? TrxRsc { get; set; }
    public string? TrxAc { get; set; }
    public string? Aid { get; set; }
    public string? Pid { get; set; }
    public string? Sid { get; set; }
    public string? OfferId { get; set; }
    public decimal? OfferAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal SubTotalAmount { get; set; }      // NOT NULL DEFAULT 0
    public decimal? TaxAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // NOT NULL DEFAULT ''
    public decimal? ExchangeRate { get; set; }
    public long SessionId { get; set; }              // NOT NULL DEFAULT 0
    public DateTime SubmissionDate { get; set; }     // NOT NULL DEFAULT GETDATE()
    public DateTime? SalesOrderDate { get; set; }
    public string Locale { get; set; } = default!;
    public string? Subject { get; set; }
    public string? Comment { get; set; }
    public DateTime InsertDate { get; set; }
    public string InsertBy { get; set; } = default!; // NOT NULL
    public DateTime ModifiedDate { get; set; }       // NOT NULL DEFAULT GETDATE()
    public string ModifiedBy { get; set; } = default!; // NOT NULL
    public byte CartOrderStatusId { get; set; }
    public byte? CurrencyId { get; set; }
    public string? CustomerProfileToken { get; set; }
    public int? CartOrderInProcessId { get; set; }
    public string? UserIp { get; set; }
    public string? Restriction { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────────
    public Currency? Currency { get; set; }
    public CartOrderStatus CartOrderStatus { get; set; } = default!;
    public ICollection<CartOrderItem> Items { get; set; } = [];
    public CartOrderPartner? CartOrderPartner { get; set; }
    public CartJson? CartJson { get; set; }
}
