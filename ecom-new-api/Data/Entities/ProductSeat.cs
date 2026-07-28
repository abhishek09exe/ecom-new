namespace ecom_new_api.Data.Entities;

/// <summary>Maps to [dbo].[product_seat].</summary>
public sealed class ProductSeat
{
    public int ProductSeatId { get; set; }
    public int ProductId { get; set; }
    public int Seats { get; set; }
    public DateTime InsertDate { get; set; }
    public string InsertBy { get; set; } = default!;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = default!;
    public int? CurrentSeats { get; set; }

    // Navigation property
    public Product? Product { get; set; }
}
