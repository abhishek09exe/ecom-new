namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [dbo].[product_years].
/// Composite PK (product_id, years). Stores the term length (in years) for a product.
/// </summary>
public sealed class ProductYears
{
    public int ProductId { get; set; }

    /// <summary>float in SQL — stored as double here, exposed as decimal on responses.</summary>
    public double Years { get; set; }

    public byte? UpgradeMonths { get; set; }
    public int? UpgradeDays { get; set; }

    // Navigation property
    public Product? Product { get; set; }
}
