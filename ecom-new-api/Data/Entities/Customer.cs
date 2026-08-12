using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("customer")]
public sealed class Customer
{
    [Key]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("opt_in")]
    public int? OptIn { get; set; }
}
