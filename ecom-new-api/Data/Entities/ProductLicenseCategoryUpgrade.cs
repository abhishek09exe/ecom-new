using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_license_category_upgrade")]
public sealed class ProductLicenseCategoryUpgrade
{
    [Key]
    [Column("product_license_category_upgrade_id")]
    public int ProductLicenseCategoryUpgradeId { get; set; }

    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }

    [Column("upgrade_license_category_id")]
    public byte UpgradeLicenseCategoryId { get; set; }

    [Column("language_code")]
    [MaxLength(2)]
    public string LanguageCode { get; set; } = default!;

    [Column("location_code")]
    [MaxLength(3)]
    public string LocationCode { get; set; } = default!;

    [Column("item_hierarchy_id")]
    public byte? ItemHierarchyId { get; set; }
}
