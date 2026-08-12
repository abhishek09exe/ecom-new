using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_seat")]
public sealed class ProductSeat
{
    [Key]
    [Column("product_seat_id")]
    public int ProductSeatId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("seats")]
    public int Seats { get; set; }

    // Navigation
    public Product? Product { get; set; }
}
