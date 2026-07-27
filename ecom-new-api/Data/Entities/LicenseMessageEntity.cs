using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a license message record (e.g., next process dates for monthly billing).
/// Maps to [ecommerce_VH14].[dbo].[license_message]
/// 
/// Section 1.3.3: Loads next_process_date from license_message table
/// Used for monthly-to-annual conversion logic (Sections 1.7, 1.7.1)
/// </summary>
[Table("license_message")]
public class LicenseMessageEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("license_message_id")]
    public int LicenseMessageId { get; set; }

    [Column("license_id")]
    public int? LicenseId { get; set; }

    [Column("message_type_id")]
    public int? MessageTypeId { get; set; }

    [Column("message_status_id")]
    public int? MessageStatusId { get; set; }

    [Column("next_process_date")]
    public DateTime? NextProcessDate { get; set; }

    [Column("created_date")]
    public DateTime? CreatedDate { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }

    [Column("message_action_id")]
    public int? MessageActionId { get; set; }

    [MaxLength(1000)]
    [Column("message_text")]
    public string? MessageText { get; set; }
}
