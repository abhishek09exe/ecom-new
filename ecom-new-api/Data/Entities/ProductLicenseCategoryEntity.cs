using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Product-to-license-category bridge.
/// Table: [product_license_category]
/// </summary>
[Table("product_license_category")]
public class ProductLicenseCategoryEntity
{
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }
}
