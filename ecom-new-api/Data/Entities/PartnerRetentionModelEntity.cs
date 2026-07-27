using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Partner-specific retention model mapping.
/// Table: [partner_retention_model]
/// </summary>
[Table("partner_retention_model")]
public class PartnerRetentionModelEntity
{
    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Column("site_id")]
    public string SiteId { get; set; } = null!;

    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }

    [Column("retention_model_id")]
    public byte? RetentionModelId { get; set; }
}