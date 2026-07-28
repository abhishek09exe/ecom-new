using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product")]
public sealed class Product
{
    [Key]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("product_description")]
    [MaxLength(255)]
    public string? ProductDescription { get; set; }

    [Column("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }

    [Column("product_type_id")]
    public int ProductTypeId { get; set; }

    [Column("product_family_id")]
    public int ProductFamilyId { get; set; }

    // Navigation
    public ProductType? ProductType { get; set; }
    public ProductFamily? ProductFamily { get; set; }
    public LicenseKeycodeType? LicenseKeycodeType { get; set; }
    public ICollection<ProductLineProduct> ProductLineProducts { get; set; } = [];
    public ICollection<ProductLicenseCategory> ProductLicenseCategories { get; set; } = [];
    public ICollection<ProductYears> ProductYears { get; set; } = [];
    public ICollection<ProductSeat> ProductSeats { get; set; } = [];
}
