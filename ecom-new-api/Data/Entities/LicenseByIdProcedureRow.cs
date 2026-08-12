using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Data.Entities;

[Keyless]
public sealed class LicenseByIdProcedureRow
{
    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("license_type_description")]
    public string? LicenseTypeDescription { get; set; }

    [Column("max_daily_activations")]
    public int? MaxDailyActivations { get; set; }

    [Column("parent_keycode")]
    public string? ParentKeycode { get; set; }

    [Column("consumed_seats")]
    public int? ConsumedSeats { get; set; }

    [Column("seats_used")]
    public int? SeatsUsed { get; set; }

    [Column("storage_gb")]
    public int? StorageGb { get; set; }

    [Column("license_attribute_description")]
    public string? LicenseAttributeDescription { get; set; }

    [Column("license_attribute_tag")]
    public string? LicenseAttributeTag { get; set; }

    [Column("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [Column("license_attribute_license_value_description")]
    public string? LicenseAttributeLicenseValueDescription { get; set; }

    [Column("license_attribute_last_modified")]
    public DateTime? LicenseAttributeLastModified { get; set; }

    [Column("oem_type")]
    public string? OemType { get; set; }

    [Column("portal_flag")]
    public int? PortalFlag { get; set; }

    [Column("renewal_count")]
    public int? RenewalCount { get; set; }

    [Column("license_origin_channel_name")]
    public string? LicenseOriginChannelName { get; set; }

    [Column("license_original_activation_date")]
    public DateTime? LicenseOriginalActivationDate { get; set; }

    [Column("email_opt_in")]
    public int? EmailOptIn { get; set; }

    [Column("license_distribution_method_code")]
    public string? LicenseDistributionMethodCode { get; set; }

    [Column("next_bill_date")]
    public DateTime? NextBillDate { get; set; }
}
