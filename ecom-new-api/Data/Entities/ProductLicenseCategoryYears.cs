using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_license_category_years")]
public sealed class ProductLicenseCategoryYears
{
    [Key]
    [Column("product_license_category_years_id")]
    public int ProductLicenseCategoryYearsId { get; set; }

    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }

    [Column("years")]
    public double Years { get; set; }

    [Column("years_description")]
    [MaxLength(20)]
    public string? YearsDescription { get; set; }

    [Column("site_display")]
    public byte? SiteDisplay { get; set; }
}