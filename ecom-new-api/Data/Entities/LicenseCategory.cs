namespace ecom_new_api.Data.Entities;

public sealed class LicenseCategory
{
    public int LicenseCategoryId { get; set; }
    public string LicenseCategoryName { get; set; } = default!;
    public string? LicenseCategoryDescription { get; set; }
    public int? MinOrderQuantity { get; set; }
    public int? MaxOrderQuantity { get; set; }
}
