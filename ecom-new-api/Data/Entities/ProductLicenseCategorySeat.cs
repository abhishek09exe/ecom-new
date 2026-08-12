using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_license_category_seat")]
public sealed class ProductLicenseCategorySeat
{
    [Key]
    [Column("product_license_category_seat_id")]
    public int ProductLicenseCategorySeatId { get; set; }

    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }

    [Column("seats")]
    public int Seats { get; set; }

    [Column("site_display")]
    public byte? SiteDisplay { get; set; }

    [Column("configuration_option")]
    public byte? ConfigurationOption { get; set; }
}