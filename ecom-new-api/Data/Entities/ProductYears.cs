using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_years")]
public sealed class ProductYears
{
    [Key]
    [Column("product_years_id")]
    public int ProductYearsId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("years")]
    public decimal Years { get; set; }

    // Navigation
    public Product? Product { get; set; }
}
