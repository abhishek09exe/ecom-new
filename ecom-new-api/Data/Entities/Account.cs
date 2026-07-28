using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("account")]
public sealed class Account
{
    [Key]
    [Column("account_id")]
    public int AccountId { get; set; }

    [Column("account_user_name")]
    [MaxLength(100)]
    public string? AccountUserName { get; set; }
}
