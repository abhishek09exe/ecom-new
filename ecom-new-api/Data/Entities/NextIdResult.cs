using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Keyless entity used solely to receive the result set from <c>EXEC usp_next_id @Type=3</c>.
/// The SP does an atomic UPDATE + SELECT on the <c>ids</c> table
/// (ids.id_type = 3 is the cart-order sequence).
///
/// Column returned by the SP: <c>next_id</c>
/// </summary>
[Keyless]
public sealed class NextIdResult
{
    [Column("next_id")]
    public int NextId { get; set; }
}
