using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a currency (USD, EUR, etc.)
/// Maps to [ecommerce_VH14].[dbo].[currency]
/// </summary>
[Table("currency")]
public class CurrencyEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("currency_id")]
    public int CurrencyId { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("currency_code")]
    public string CurrencyCode { get; set; } = null!;

    [MaxLength(255)]
    [Column("currency_name")]
    public string? CurrencyName { get; set; }

    // Navigation properties
    public virtual ICollection<CartOrderEntity> CartOrders { get; set; } = [];
}
