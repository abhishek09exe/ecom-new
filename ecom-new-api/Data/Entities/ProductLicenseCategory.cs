namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [dbo].[product_license_category].
/// Join table — composite PK (license_category_id, product_id).
/// </summary>
public sealed class ProductLicenseCategory
{
    public int ProductId { get; set; }
    public int LicenseCategoryId { get; set; }
    public int? CurrentLicenseCategoryId { get; set; }

    // Navigation properties
    public Product? Product { get; set; }
    public LicenseCategory? LicenseCategory { get; set; }
}
