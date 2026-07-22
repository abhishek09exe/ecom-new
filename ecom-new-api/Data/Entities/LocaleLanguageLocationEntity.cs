using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents locale to language/location code mappings.
/// Maps to [ecommerce_VH14].[dbo].[locale_language_location] or result of [fn_locale_to_lang_loc]
/// 
/// Section 1.2.1: Translates @locale (e.g., 'en_US') to language_code and location_code
/// Used for product line lookups (Section 1.9) and order context
/// </summary>
[Table("locale_language_location")]
public class LocaleLanguageLocationEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("locale_language_location_id")]
    public int LocaleLanguageLocationId { get; set; }

    [MaxLength(20)]
    [Column("locale")]
    public string? Locale { get; set; }

    [MaxLength(10)]
    [Column("language_code")]
    public string? LanguageCode { get; set; }

    [MaxLength(10)]
    [Column("location_code")]
    public string? LocationCode { get; set; }

    [Column("created_date")]
    public DateTime? CreatedDate { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }
}
