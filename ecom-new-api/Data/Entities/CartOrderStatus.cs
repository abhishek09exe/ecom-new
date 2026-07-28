namespace ecom_new_api.Data.Entities;

public sealed class CartOrderStatus
{
    public byte CartOrderStatusId { get; set; }
    public string StatusDescription { get; set; } = default!;
    public DateTime InsertDate { get; set; }
    public string InsertBy { get; set; } = default!;

    // Navigation
    public ICollection<CartOrder> CartOrders { get; set; } = [];
}
