using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Partner-specific usage pricing model mapping.
/// Table: [partner_usage_pricing_model]
/// </summary>
[Table("partner_usage_pricing_model")]
public class PartnerUsagePricingModelEntity
{
    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Column("site_id")]
    public string SiteId { get; set; } = null!;

    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }

    [Column("usage_pricing_model_id")]
    public byte? UsagePricingModelId { get; set; }
}