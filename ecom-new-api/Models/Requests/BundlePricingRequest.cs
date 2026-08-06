using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Models.Requests;

public class BundlePricingRequest
{
    [Required]
    [FromQuery(Name = "locale")]
    public string Locale { get; set; } = "en_US";

    [FromQuery(Name = "license_keycode_type_id")]
    public int LicenseKeycodeTypeId { get; set; } = 1;

    [Required, MinLength(1)]
    public List<BundlePricingItem> Items { get; set; } = new();
}

public class BundlePricingItem
{
    [Required]
    public string LicenseCategoryName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int LicenseSeats { get; set; }

    public decimal Years { get; set; } = 1;

    public string? MessageKey { get; set; }

    public int LicenseAttributeLicenseValue { get; set; } = 1;

    public int LicenseKeycodeTypeId { get; set; }

    public int? StorageGb { get; set; }

    public int? RetentionModelId { get; set; }

    public List<BundleModule> Modules { get; set; } = new();
}

public class BundleModule
{
    [Required]
    public string LicenseCategoryName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int LicenseSeats { get; set; }

    public decimal Years { get; set; } = 1;

    public string? CategoryTypeName { get; set; }

    public int? StorageGb { get; set; }
}
