using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_license_category")]
public sealed class ProductLicenseCategory
{
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }

    // Navigation
    public Product? Product { get; set; }
    public LicenseCategory? LicenseCategory { get; set; }
}
