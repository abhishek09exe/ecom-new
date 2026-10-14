using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("lu_countries")]
public sealed class LuCountry
{
    [Key]
    [Column("country_id")]
    public short CountryId { get; set; }

    [Column("iso")]
    [MaxLength(2)]
    public string? Iso { get; set; }

    [Column("iso3")]
    [MaxLength(3)]
    public string? Iso3 { get; set; }
}