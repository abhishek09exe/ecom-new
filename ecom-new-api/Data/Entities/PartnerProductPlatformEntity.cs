using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Partner-specific product platform mapping.
/// Table: [partner_product_platform]
/// </summary>
[Table("partner_product_platform")]
public class PartnerProductPlatformEntity
{
    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Column("site_id")]
    public string SiteId { get; set; } = null!;

    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }

    [Column("product_platform_id")]
    public byte? ProductPlatformId { get; set; }
}