namespace ecom_new_api.Data.Entities;

/// <summary>
/// POCO mapped from the usp_trial_insert_trial_registration result set.
/// Not an EF entity — the procedure returns two different shapes and is read with a DataReader.
///
/// IMPORTANT: the success path returns NO response_code column; response_code is only present
/// on the duplicate (-110) and error (-200) paths. A null ResponseCode therefore means success.
/// </summary>
public sealed class TrialRegistrationProcedureResult
{
    public int? ResponseCode { get; set; }
    public string? Message { get; set; }
    public int? TrialRegistrationId { get; set; }
    public string? RequestType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CustomerEmail { get; set; }
    public int? ProductLineId { get; set; }
    public string? LicenseCategoryName { get; set; }
    public int? LicenseCategoryId { get; set; }
    public string? LicenseModuleList { get; set; }
    public string? LicenseType { get; set; }
    public int? LicenseTypeId { get; set; }
    public int? CapabilityActivationDays { get; set; }
    public int? LicenseSeats { get; set; }
    public int? StorageGb { get; set; }
    public string? LicenseDistributionMethodCode { get; set; }
}
