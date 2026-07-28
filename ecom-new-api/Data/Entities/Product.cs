namespace ecom_new_api.Data.Entities;

public sealed class Product
{
    public int ProductId { get; set; }
    public string ProductDescription { get; set; } = default!; // NOT NULL
    public int ProductTypeId { get; set; }                     // NOT NULL
    public int? ProductFamilyId { get; set; }
    public int? ProductLifecycleId { get; set; }
    public int? LicenseKeycodeTypeId { get; set; }
    public int? RootProductId { get; set; }
    public int UsesKeycode { get; set; }                       // NOT NULL DEFAULT 0
    public int? CdProductId { get; set; }
    public decimal? RetailPrice { get; set; }
    public string? Basename { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────────

    /// <summary>product.product_type_id → product_type</summary>
    public ProductType? ProductType { get; set; }

    /// <summary>product.product_family_id → product_family</summary>
    public ProductFamily? ProductFamily { get; set; }

    /// <summary>product.license_keycode_type_id → license_keycode_type</summary>
    public LicenseKeycodeType? LicenseKeycodeType { get; set; }

    /// <summary>product_license_category join rows for this product (→ license_category)</summary>
    public ICollection<ProductLicenseCategory> ProductLicenseCategories { get; set; } = [];

    /// <summary>product_years rows for this product (one-to-many; usually one row)</summary>
    public ICollection<ProductYears> ProductYears { get; set; } = [];

    /// <summary>product_seat rows for this product (usually one row)</summary>
    public ICollection<ProductSeat> ProductSeats { get; set; } = [];

    /// <summary>product_line_product join rows for this product (→ product_line)</summary>
    public ICollection<ProductLineProduct> ProductLineProducts { get; set; } = [];
}
