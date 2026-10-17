using System.Data;
using System.Data.Common;
using System.Text.Json;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories.Trials;

/// <summary>
/// Wrapper for dbo.usp_trial_insert_trial_registration — the single database entry point for
/// trial registrations (PHP equivalent: procedures/trial/insert/TrialRegistration.php).
///
/// The procedure is driven almost entirely by @trial_registration_json (parsed with OPENJSON);
/// the scalar parameters are kept for the legacy non-JSON path and are still sent, exactly as
/// the PHP wrapper does.
///
/// A DataReader is used instead of Database.SqlQueryRaw because the procedure returns two
/// different result-set shapes: the success path has no response_code/message columns, while the
/// duplicate (-110) path has no product columns. SqlQueryRaw requires every mapped column to be
/// present in the result set and would throw on one of the two shapes.
/// </summary>
public sealed class TrialRegistrationRepository : ITrialRegistrationRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<TrialRegistrationRepository> _logger;

    public TrialRegistrationRepository(AppDbContext db, ILogger<TrialRegistrationRepository> logger,
        LicenseServiceStagingRepository staging)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<int>> InsertModuleLicenseRecordsAsync(
        int licenseId, LicenseCreationContext creationContext,
        DateTime originalAuditTimestamp, string originalAuditUser, CancellationToken ct = default)
    {
        if (licenseId <= 0 || creationContext.PrimaryCategoryName != "SAEP" ||
            creationContext.ProductLineId != 300 || !creationContext.IsTrial ||
            creationContext.LicenseKeycodeTypeId != 3 || creationContext.Seats != 1)
            throw new InvalidOperationException("Module record creation requires an existing SAEP master trial license context.");
        if (originalAuditUser is null || originalAuditUser.Length > 200)
            throw new InvalidOperationException("The original audit user must fit the SQL varchar(200) column.");
        if (creationContext.Modules.Count == 0)
            return Array.Empty<int>();
        if (_db.ChangeTracker.Entries().Any(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Module record creation requires a context without unrelated pending writes.");

        var records = creationContext.Modules.Select(module => new LicenseModuleLicense
        {
            LicenseId = licenseId,
            LicenseModuleId = module.LicenseModuleId,
            LicenseModuleTypeId = module.LicenseModuleTypeId,
            ModuleSeats = module.Seats,
            StartDate = module.StartDate,
            EndDate = module.ExpirationDate,
            LicenseStatusId = 1,
            LastModifiedDate = originalAuditTimestamp,
            LastModifiedBy = originalAuditUser
        }).ToList();
        _db.LicenseModuleLicense.AddRange(records);
        await _db.SaveChangesAsync(ct);
        return records.Select(record => record.LicenseModuleLicenseId).ToList();
    }

    public async Task<LicensePrimaryCreationResult> InsertPrimaryLicenseRecordsAsync(
        int licenseId, LicenseCreationContext creationContext,
        DateTime originalAuditTimestamp, string originalAuditUser, CancellationToken ct = default)
    {
        var activationDays = creationContext.StartDate.HasValue && creationContext.ExpirationDate.HasValue
            ? (int?)(creationContext.ExpirationDate.Value.Date - creationContext.StartDate.Value.Date).TotalDays
            : null;
        if (licenseId <= 0 || creationContext.PrimaryCategoryName != "SAEP" ||
            creationContext.ProductLineId != 300 || !creationContext.IsTrial ||
            creationContext.Seats != 1 || activationDays != 30)
            throw new InvalidOperationException("Primary records require an existing license ID and a SAEP 30-day, one-seat trial context.");
        if (originalAuditUser is null || originalAuditUser.Length > 50)
            throw new InvalidOperationException("The original audit user must fit the SQL varchar(50) columns.");
        if (_db.ChangeTracker.Entries().Any(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Primary record creation requires a context without unrelated pending writes.");

        var category = new LicenseCategoryLicense
        {
            LicenseId = licenseId,
            LicenseCategoryId = creationContext.LicenseCategoryId,
            StartDate = creationContext.StartDate,
            EndDate = creationContext.ExpirationDate,
            LastModifiedDate = originalAuditTimestamp,
            LastModifiedBy = originalAuditUser
        };
        _db.LicenseCategoryLicense.Add(category);
        await _db.SaveChangesAsync(ct);

        var capabilities = creationContext.CapabilityIds.Select(capabilityId => new LicenseCapability
        {
            LicenseId = licenseId,
            CapabilityId = capabilityId,
            CapabilityTypeId = creationContext.TrialCapabilityTypeId,
            CapabilityActivationDays = activationDays,
            CapabilityActivationDate = creationContext.StartDate,
            CapabilityExpirationDate = creationContext.ExpirationDate,
            InsertDate = originalAuditTimestamp,
            InsertBy = originalAuditUser,
            ModifiedDate = originalAuditTimestamp,
            ModifiedBy = originalAuditUser
        }).ToList();
        _db.LicenseCapability.AddRange(capabilities);
        await _db.SaveChangesAsync(ct);

        var seat = new LicenseSeat
        {
            LicenseId = licenseId,
            LicenseSeats = creationContext.Seats,
            StartDate = creationContext.StartDate,
            EndDate = creationContext.ExpirationDate,
            InsertDate = originalAuditTimestamp,
            InsertBy = originalAuditUser,
            ModifiedDate = originalAuditTimestamp,
            ModifiedBy = originalAuditUser
        };
        _db.LicenseSeat.Add(seat);
        await _db.SaveChangesAsync(ct);

        return new LicensePrimaryCreationResult(category.LicenseCategoryLicenseId,
            capabilities.Select(capability => capability.LicenseCapabilityId).ToList(), seat.LicenseSeatId);
    }

    public async Task<Guid> InsertLicenseKeyAsync(
        int licenseId, string serviceJson, CancellationToken ct = default)
    {
        if (licenseId <= 0)
            throw new InvalidOperationException("License key creation requires an existing license ID.");

        using var document = JsonDocument.Parse(serviceJson);
        string? salesforceLicenseId = null;
        if (document.RootElement.TryGetProperty("sfdc", out var sfdc) && sfdc.ValueKind == JsonValueKind.Object &&
            sfdc.TryGetProperty("salesforce_license_id", out var salesforce) &&
            salesforce.ValueKind != JsonValueKind.Null)
        {
            var scalar = salesforce.ValueKind == JsonValueKind.String ? salesforce.GetString() :
                salesforce.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False
                    ? salesforce.GetRawText() : null;
            salesforceLicenseId = scalar is null ? null : scalar[..Math.Min(scalar.Length, 18)].PadRight(18);
        }

        var key = new LicenseKey { LicenseId = licenseId, SalesforceLicenseId = salesforceLicenseId };
        _db.LicenseKey.Add(key);
        await _db.SaveChangesAsync(ct);
        return key.Key;
    }

    public async Task<int?> InsertLicenseFeatureLicenseAsync(
        int licenseId, LicenseCreationContext creationContext, CancellationToken ct = default)
    {
        if (licenseId <= 0)
            throw new InvalidOperationException("License feature creation requires an existing license ID.");
        if (creationContext.LicenseKeycodeTypeId != 3)
            return null;

        var feature = new LicenseFeatureLicense { LicenseId = licenseId, LicenseFeatureId = 8 };
        _db.LicenseFeatureLicense.Add(feature);
        await _db.SaveChangesAsync(ct);
        return feature.LicenseFeatureLicenseId;
    }

    public async Task<LicenseCreationResult> InsertLicenseRecordsInLegacyOrderAsync(
        int licenseId, LicenseCreationContext creationContext, string serviceJson,
        DateTime originalAuditTimestamp, string originalAuditUser, CancellationToken ct = default)
    {
        var primary = await InsertPrimaryLicenseRecordsAsync(
            licenseId, creationContext, originalAuditTimestamp, originalAuditUser, ct);
        var modules = await InsertModuleLicenseRecordsAsync(
            licenseId, creationContext, originalAuditTimestamp, originalAuditUser, ct);
        var key = await InsertLicenseKeyAsync(licenseId, serviceJson, ct);
        var feature = await InsertLicenseFeatureLicenseAsync(licenseId, creationContext, ct);

        return new LicenseCreationResult(primary.LicenseCategoryLicenseId, primary.LicenseCapabilityIds,
            primary.LicenseSeatId, modules, key, feature);
    }

    public async Task<int> InsertLicenseHeaderAsync(
        LicenseCreationContext creationContext, string preparedKeycode, string serviceJson,
        CancellationToken ct = default)
    {
        if (creationContext.PrimaryCategoryName != "SAEP" || creationContext.ProductLineId != 300 ||
            !creationContext.IsTrial || creationContext.LicenseKeycodeTypeId != 3 ||
            creationContext.LicenseTypeId != 2 || creationContext.Seats != 1 ||
            creationContext.CapabilityActivationDays != 30)
            throw new InvalidOperationException("License header creation supports only the resolved SAEP 30-day, one-seat master trial context.");
        if (string.IsNullOrEmpty(preparedKeycode) || preparedKeycode.Length > 40)
            throw new InvalidOperationException("A prepared keycode of at most 40 characters is required.");

        using var document = JsonDocument.Parse(serviceJson);
        var root = document.RootElement;
        var customer = root.TryGetProperty("customer", out var customerObject) ? customerObject : default;

        static string? Text(JsonElement element, string name, int length)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) ||
                value.ValueKind == JsonValueKind.Null)
                return null;
            var text = value.GetString();
            return text is not null && text.Length > length ? text[..length] : text;
        }

        static int? Number(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) ||
                value.ValueKind == JsonValueKind.Null)
                return null;
            return value.GetInt32();
        }

        var insertBy = Text(root, "account_name", 20) ?? "license";
        var email = Text(customer, "customer_email", 100);
        var vendorCustomerCode = Text(customer, "vendor_customer_code", 100);
        if (email is null && vendorCustomerCode is null && root.TryGetProperty("partner", out var partner))
            vendorCustomerCode = Text(partner, "partner_account_code", 100);

        var parameters = new List<SqlParameter>();
        void Add(string name, SqlDbType type, int size, object? value)
        {
            var parameter = size > 0 ? new SqlParameter(name, type, size) : new SqlParameter(name, type);
            parameter.Value = value ?? DBNull.Value;
            parameters.Add(parameter);
        }

        Add("@customer_email", SqlDbType.VarChar, 100, email);
        Add("@first_name", SqlDbType.NVarChar, 225, Text(customer, "first_name", 225));
        Add("@last_name", SqlDbType.NVarChar, 225, Text(customer, "last_name", 225));
        Add("@company_name", SqlDbType.NVarChar, 255, Text(customer, "company_name", 255));
        Add("@company_type_name", SqlDbType.VarChar, 50, Text(customer, "company_type_name", 20));
        Add("@phone_number", SqlDbType.VarChar, 64, Text(customer, "phone_number", 64));
        Add("@address_1", SqlDbType.NVarChar, 255, Text(customer, "address_1", 255));
        Add("@address_2", SqlDbType.NVarChar, 255, Text(customer, "address_2", 255));
        Add("@city", SqlDbType.NVarChar, 130, Text(customer, "city", 130));
        Add("@state", SqlDbType.NVarChar, 2, Text(customer, "state", 2));
        Add("@postal_code", SqlDbType.NVarChar, 32, Text(customer, "postal_code", 32));
        Add("@country", SqlDbType.VarChar, 75, Text(customer, "country", 75));
        Add("@vendor_id", SqlDbType.Int, 0, Number(customer, "vendor_id"));
        Add("@vendor_customer_code", SqlDbType.VarChar, 100, vendorCustomerCode);
        Add("@partner_id", SqlDbType.Int, 0, Number(customer, "partner_id"));
        Add("@opt_in", SqlDbType.Int, 0, Number(customer, "opt_in"));
        Add("@salesforce_account_id", SqlDbType.Char, 18, Text(customer, "salesforce_account_id", 18));
        Add("@salesforce_contact_id", SqlDbType.Char, 18, Text(customer, "salesforce_contact_id", 18));
        Add("@transaction_user", SqlDbType.VarChar, 20, insertBy);
        Add("@customer_id_input", SqlDbType.Int, 0, null);
        Add("@company_id_input", SqlDbType.Int, 0, null);
        var customerIdOutput = new SqlParameter("@customer_id", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output,
            Value = DBNull.Value
        };
        parameters.Add(customerIdOutput);

        var timestamp = await _db.Database.SqlQuery<DateTime>($"SELECT GETDATE() AS [Value]").SingleAsync(ct);
        if (email is not null || vendorCustomerCode is not null)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_customer_insert_customer_service " +
                "@customer_email, @first_name, @last_name, @company_name, @company_type_name, " +
                "@phone_number, @address_1, @address_2, @city, @state, @postal_code, @country, " +
                "@vendor_id, @vendor_customer_code, @partner_id, @opt_in, @salesforce_account_id, " +
                "@salesforce_contact_id, @transaction_user, @customer_id_input, @company_id_input, " +
                "@customer_id OUTPUT", parameters, ct);
        }

        var license = new License
        {
            Keycode = preparedKeycode,
            CustomerId = customerIdOutput.Value is DBNull or null ? null : Convert.ToInt32(customerIdOutput.Value),
            ProductLineId = creationContext.ProductLineId,
            LicenseStatusId = 1,
            LicenseTypeId = creationContext.LicenseTypeId,
            LicenseDistributionMethodId = creationContext.LicenseDistributionMethodId,
            LicenseKeycodeTypeId = creationContext.LicenseKeycodeTypeId,
            InsertDate = timestamp,
            ModifiedDate = timestamp,
            InsertBy = insertBy,
            ModifiedBy = insertBy
        };
        _db.License.Add(license);
        await _db.SaveChangesAsync(ct);
        return license.LicenseId;
    }

    public async Task<LicenseCreationContext> ResolveLicenseCreationContextAsync(
        string serviceJson, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Deserialize<LicenseCreationPayload>(serviceJson)
            ?? throw new InvalidOperationException("License creation JSON is required.");
        var primaries = payload.Categories?.Where(category => category.Name is not null && category.HierarchyId == 1).ToList();
        if (primaries is null || primaries.Count != 1)
            throw new InvalidOperationException("Exactly one primary license category is required.");
        var primary = primaries[0];
        if (primary.Name != "SAEP" || primary.CategoryType != "trial" || primary.Seats != 1 ||
            primary.Days != 30 || payload.KeycodeTypeId != 3 || !string.IsNullOrEmpty(payload.ParentKeycode))
            throw new InvalidOperationException("Creation context supports only a standalone SAEP 30-day, one-seat trial with keycode type 3.");

        var categoryIds = await _db.LicenseCategory.AsNoTracking()
            .Where(category => category.LicenseCategoryName == primary.Name)
            .Select(category => category.LicenseCategoryId).ToListAsync(ct);
        var categoryId = RequireOne(categoryIds, "primary license category");
        var productLines = await _db.LicenseCategoryProductLine.AsNoTracking()
            .Where(mapping => mapping.LicenseCategoryId == categoryId &&
                mapping.LanguageCode.ToUpper() == "EN" && mapping.LocationCode.ToUpper() == "USA")
            .Select(mapping => mapping.ProductLineId).ToListAsync(ct);
        var productLineId = RequireOne(productLines, "EN/USA product-line mapping");
        if (productLineId != 300)
            throw new InvalidOperationException("SAEP creation context requires product line 300.");

        var capabilityIds = await _db.LicenseCategoryCapability.AsNoTracking()
            .Where(mapping => mapping.LicenseCategoryId == categoryId)
            .Select(mapping => mapping.CapabilityId).ToListAsync(ct);
        if (capabilityIds.Count == 0 || capabilityIds.Distinct().Count() != capabilityIds.Count)
            throw new InvalidOperationException("Missing or duplicate primary capability mappings.");
        var capabilityTypes = await _db.CapabilityType.AsNoTracking()
            .Where(type => type.CapabilityTypeDescription == primary.CategoryType)
            .Select(type => type.CapabilityTypeId).ToListAsync(ct);
        var trialCapabilityTypeId = RequireOne(capabilityTypes, "trial capability type");

        int? distributionId = null;
        if (!string.IsNullOrEmpty(payload.DistributionCode))
        {
            var explicitMethods = await _db.LicenseDistributionMethod.AsNoTracking()
                .Where(method => method.LicenseDistributionMethodCode == payload.DistributionCode)
                .Select(method => method.LicenseDistributionMethodId).ToListAsync(ct);
            if (explicitMethods.Count > 1)
                throw new InvalidOperationException("Ambiguous explicit distribution method.");
            distributionId = explicitMethods.Count == 1 ? explicitMethods[0] : null;
        }
        if (distributionId is null)
        {
            var mappings = _db.ProductLineLicenseDistributionMethod.AsNoTracking()
                .Where(mapping => mapping.ProductLineId == productLineId &&
                    mapping.LicenseKeycodeTypeId == payload.KeycodeTypeId && mapping.Trial == 1);
            var categoryMethods = await mappings.Where(mapping => mapping.LicenseCategoryId == categoryId)
                .Select(mapping => mapping.LicenseDistributionMethodId).ToListAsync(ct);
            var candidates = categoryMethods.Count > 0 ? categoryMethods :
                await mappings.Where(mapping => mapping.LicenseCategoryId == null)
                    .Select(mapping => mapping.LicenseDistributionMethodId).ToListAsync(ct);
            distributionId = RequireOne(candidates, "trial distribution-method mapping");
        }
        var distributionCodes = await _db.LicenseDistributionMethod.AsNoTracking()
            .Where(method => method.LicenseDistributionMethodId == distributionId)
            .Select(method => method.LicenseDistributionMethodCode).ToListAsync(ct);
        var distributionCode = RequireOne(distributionCodes, "resolved distribution method");

        var modules = new List<LicenseCreationModule>();
        foreach (var secondary in payload.Categories!.Where(category => category.Name is not null && category.HierarchyId == 2))
        {
            var secondaryIds = await _db.LicenseCategory.AsNoTracking()
                .Where(category => category.LicenseCategoryName == secondary.Name)
                .Select(category => category.LicenseCategoryId).ToListAsync(ct);
            var secondaryId = RequireOne(secondaryIds, $"module category {secondary.Name}");
            var moduleTypes = await _db.LicenseModuleType.AsNoTracking()
                .Where(type => type.LicenseModuleTypeName == secondary.CategoryType)
                .Select(type => type.LicenseModuleTypeId).ToListAsync(ct);
            var moduleTypeId = RequireOne(moduleTypes, $"module type {secondary.CategoryType}");
            var configuredModules = await (
                from mapping in _db.LicenseModuleCategory.AsNoTracking()
                join module in _db.LicenseModule.AsNoTracking() on mapping.LicenseModuleId equals module.LicenseModuleId
                where mapping.LicenseCategoryId == secondaryId && module.LicenseModuleClassId == 3
                select new { module.LicenseModuleId, module.LicenseModuleCode }).ToListAsync(ct);
            if (configuredModules.Count == 0 || configuredModules.Select(module => module.LicenseModuleId).Distinct().Count() != configuredModules.Count)
                throw new InvalidOperationException($"Missing or duplicate class-3 module configuration for {secondary.Name}.");
            modules.AddRange(configuredModules.Select(module => new LicenseCreationModule(
                secondary.Name!, secondaryId, module.LicenseModuleId, module.LicenseModuleCode,
                moduleTypeId, secondary.Seats, secondary.StartDate, secondary.ExpirationDate)));
        }

        return new LicenseCreationContext(primary.Name!, categoryId, productLineId,
            productLineId == 300 && payload.KeycodeTypeId == 3 ? 2 : 1,
            payload.KeycodeTypeId.Value, true, primary.Seats, primary.StartDate, primary.ExpirationDate,
            primary.ExpirationDate is null && primary.Days is null ? 30 : null,
            primary.StartDate.HasValue && primary.ExpirationDate.HasValue
                ? (int)(primary.ExpirationDate.Value.Date - primary.StartDate.Value.Date).TotalDays : null,
            trialCapabilityTypeId, distributionId.Value, distributionCode, capabilityIds, modules);
    }

    private static T RequireOne<T>(IReadOnlyList<T> values, string configuration)
    {
        if (values.Count != 1)
            throw new InvalidOperationException($"Missing or ambiguous {configuration}.");
        return values[0];
    }

    public async Task<TrialRegistrationProcedureResult?> InsertTrialRegistrationAsync(
        MultiTrialSubmissionRequest request,
        string trialRegistrationJson,
        CancellationToken ct = default)
    {
        var selectedProduct = await SelectTrialProductAsync(
            request.LicenseCategoryName,
            request.LanguageCode,
            request.LocationCode,
            request.TrialDays,
            request.LicenseKeycodeTypeId,
            ct);

        if (selectedProduct is not null)
        {
            selectedProduct = await ApplyTrialDiscountsAsync(
                selectedProduct,
                request.LicenseSeats,
                cancellationToken: ct);
        }

        if (selectedProduct is null && string.IsNullOrWhiteSpace(request.PartnerProductId))
        {
            _logger.LogWarning(
                "No trial product matched category={Category} language={Language} location={Location} days={Days} keycode_type_id={KeycodeTypeId}",
                request.LicenseCategoryName,
                request.LanguageCode,
                request.LocationCode,
                request.TrialDays,
                request.LicenseKeycodeTypeId);
            return null;
        }

        var connection = _db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "dbo.usp_trial_insert_trial_registration";

        AddParameters(command, request, trialRegistrationJson);

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                _logger.LogError(
                    "usp_trial_insert_trial_registration returned no result set for license_category_name={Category}",
                    request.LicenseCategoryName);
                return null;
            }

            return Map(reader);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    public async Task<TrialInsertProcedureResult?> InsertTrialAsync(
        int trialRegistrationId,
        string? keycode,
        CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "dbo.usp_trial_insert_trial";

        command.Parameters.Add(new SqlParameter("@trial_registration_id", SqlDbType.Int)
        {
            Value = trialRegistrationId
        });
        command.Parameters.Add(new SqlParameter("@keycode", SqlDbType.VarChar, 40)
        {
            Value = (object?)keycode ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@account_order_json", SqlDbType.VarChar, -1)
        {
            Value = DBNull.Value
        });

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                _logger.LogError(
                    "usp_trial_insert_trial returned no result set for trial_registration_id={TrialRegistrationId}",
                    trialRegistrationId);
                return null;
            }

            var trialIdOrdinal = reader.GetOrdinal("trial_id");
            if (reader.IsDBNull(trialIdOrdinal))
            {
                _logger.LogError(
                    "usp_trial_insert_trial returned a null trial_id for trial_registration_id={TrialRegistrationId}",
                    trialRegistrationId);
                return null;
            }

            var trialId = reader.GetInt32(trialIdOrdinal);

            await reader.CloseAsync();

            await using var keycodeCommand = connection.CreateCommand();
            keycodeCommand.CommandType = CommandType.Text;
            keycodeCommand.CommandText = @"
                SELECT r.keycode, l.license_id, j.trial_registration_json
                FROM dbo.trial_registration r
                INNER JOIN dbo.trial t ON t.trial_registration_id = r.trial_registration_id
                INNER JOIN dbo.license l ON l.license_id = t.license_id AND l.keycode = r.keycode
                INNER JOIN dbo.trial_registration_json j ON j.trial_registration_id = r.trial_registration_id
                WHERE r.trial_registration_id = @trial_registration_id AND t.trial_id = @trial_id";
            keycodeCommand.Parameters.Add(new SqlParameter("@trial_registration_id", SqlDbType.Int)
            {
                Value = trialRegistrationId
            });
            keycodeCommand.Parameters.Add(new SqlParameter("@trial_id", SqlDbType.Int)
            {
                Value = trialId
            });

            await using var persisted = await keycodeCommand.ExecuteReaderAsync(ct);
            if (!await persisted.ReadAsync(ct))
                return null;

            var persistedKeycode = persisted.IsDBNull(0) ? null : persisted.GetString(0);
            var licenseId = persisted.GetInt32(1);
            var registrationJson = persisted.GetString(2);
            await persisted.CloseAsync();

            if (!await VerifyPersistedModulesAsync(licenseId, registrationJson, ct))
            {
                _logger.LogError("Trial module persistence verification failed for trial registration {TrialRegistrationId}",
                    trialRegistrationId);
                return null;
            }

            return new TrialInsertProcedureResult
            {
                TrialId = trialId,
                FinalKeycode = persistedKeycode
            };
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    public async Task<bool> VerifyPersistedModulesAsync(
        int licenseId, string registrationJson, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Deserialize<LicenseCreationPayload>(registrationJson);
        if (payload?.Categories is null)
            return false;

        foreach (var category in payload.Categories.Where(category => category.HierarchyId == 2 &&
            (string.Equals(category.Name, "SDNS", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(category.Name, "SECA", StringComparison.OrdinalIgnoreCase))))
        {
            var categoryName = category.Name!.ToUpperInvariant();
            var expectedModuleIds = await (
                from mapping in _db.LicenseModuleCategory.AsNoTracking()
                join module in _db.LicenseModule.AsNoTracking() on mapping.LicenseModuleId equals module.LicenseModuleId
                join configuredCategory in _db.LicenseCategory.AsNoTracking()
                    on mapping.LicenseCategoryId equals configuredCategory.LicenseCategoryId
                where configuredCategory.LicenseCategoryName.ToUpper() == categoryName && module.LicenseModuleClassId == 3
                select module.LicenseModuleId).Distinct().ToListAsync(ct);
            if (expectedModuleIds.Count == 0)
                return false;

            var trialTypeIds = await _db.LicenseModuleType.AsNoTracking()
                .Where(type => type.LicenseModuleTypeName == "trial")
                .Select(type => type.LicenseModuleTypeId).ToListAsync(ct);
            if (trialTypeIds.Count != 1)
                return false;

            var trialTypeId = trialTypeIds[0];
            var actualModules = await _db.LicenseModuleLicense.AsNoTracking()
                .Where(module => module.LicenseId == licenseId && expectedModuleIds.Contains(module.LicenseModuleId))
                .ToListAsync(ct);
            foreach (var moduleId in expectedModuleIds)
            {
                var matches = actualModules.Where(module => module.LicenseModuleId == moduleId &&
                    module.LicenseModuleTypeId == trialTypeId && module.ModuleSeats == category.Seats &&
                    module.StartDate == category.StartDate && module.EndDate == category.ExpirationDate &&
                    module.LicenseStatusId == 1).ToList();
                if (matches.Count != 1)
                    return false;
            }
        }

        return true;
    }

    public async Task<TrialProductSelection?> SelectTrialProductAsync(
        string? licenseCategoryName,
        string? languageCode,
        string? locationCode,
        int trialDays,
        int licenseKeycodeTypeId,
        CancellationToken ct = default)
    {
        var resolvedLanguageCode = string.IsNullOrEmpty(languageCode) ? "EN" : languageCode;
        var resolvedLocationCode = locationCode;

        if (!string.IsNullOrEmpty(resolvedLocationCode) && resolvedLocationCode.Length == 2)
        {
            var iso2 = resolvedLocationCode.ToUpper();
            resolvedLocationCode = await _db.LuCountry
                .AsNoTracking()
                .Where(country => country.Iso != null && country.Iso.ToUpper() == iso2)
                .Select(country => country.Iso3)
                .FirstOrDefaultAsync(ct);
        }

        resolvedLocationCode = string.IsNullOrEmpty(resolvedLocationCode) ? "USA" : resolvedLocationCode;
        var language = resolvedLanguageCode.ToUpper();
        var location = resolvedLocationCode.ToUpper();

        var candidates =
            from category in _db.LicenseCategory.AsNoTracking()
            join categoryLine in _db.LicenseCategoryProductLine.AsNoTracking()
                on category.LicenseCategoryId equals categoryLine.LicenseCategoryId
            join productCategory in _db.ProductLicenseCategory.AsNoTracking()
                on category.LicenseCategoryId equals productCategory.LicenseCategoryId
            join product in _db.Product.AsNoTracking()
                on productCategory.ProductId equals product.ProductId
            join productLine in _db.ProductLineProduct.AsNoTracking()
                on product.ProductId equals productLine.ProductId
            join productYears in _db.ProductYears.AsNoTracking()
                on product.ProductId equals productYears.ProductId
            join capability in _db.ProductCapability.AsNoTracking()
                on new { product.ProductId, CapabilityId = category.BaseCapabilityId }
                equals new { capability.ProductId, CapabilityId = (int?)capability.CapabilityId }
            join pricingRow in _db.ProductPricing.AsNoTracking()
                on new { product.ProductId, LanguageCode = language, LocationCode = location }
                equals new
                {
                    pricingRow.ProductId,
                    LanguageCode = pricingRow.LanguageCode!.ToUpper(),
                    LocationCode = pricingRow.LocationCode!.ToUpper()
                }
                into pricingRows
            from pricing in pricingRows.DefaultIfEmpty()
            join currencyRow in _db.Currency.AsNoTracking()
                on (pricing == null ? null : (int?)pricing.CurrencyId)
                equals (int?)currencyRow.CurrencyId
                into currencyRows
            from currency in currencyRows.DefaultIfEmpty()
            where category.LicenseCategoryName == licenseCategoryName
                && categoryLine.LanguageCode.ToUpper() == language
                && categoryLine.LocationCode.ToUpper() == location
                && categoryLine.CartTypeId == 2
                && product.ProductTypeId == 1
                && product.LicenseKeycodeTypeId == licenseKeycodeTypeId
                && product.UsesKeycode == 1
                && productLine.ProductLineId == categoryLine.ProductLineId
                && productYears.Years == 0
                && capability.CapabilityId == category.BaseCapabilityId
                && capability.CapabilityActivationDays == trialDays
            select new TrialProductSelection
            {
                ProductId = product.ProductId,
                ProductDescription = product.ProductDescription,
                ProductTypeId = product.ProductTypeId,
                LicenseCategoryName = category.LicenseCategoryName,
                LicenseCategoryId = category.LicenseCategoryId,
                ProductLineId = categoryLine.ProductLineId,
                LanguageCode = language,
                LocationCode = location,
                Years = productYears.Years,
                CapabilityId = capability.CapabilityId,
                CapabilityTypeId = capability.CapabilityTypeId,
                CapabilityActivationDays = capability.CapabilityActivationDays,
                RetailPrice = pricing == null ? null : (decimal?)pricing.RetailPrice,
                EducationNonProfitPrice = pricing == null ? null : pricing.EducationNonProfitPrice,
                GovernmentPrice = pricing == null ? null : pricing.GovernmentPrice,
                CurrencyId = currency == null ? null : (int?)currency.CurrencyId,
                CurrencyCode = currency == null ? null : currency.CurrencyCode,
                CurrencyDescription = currency == null ? null : currency.CurrencyDescription,
                SymbolHtml = currency == null ? null : currency.SymbolHtml,
                SymbolUtf8 = currency == null ? null : currency.SymbolUtf8,
                SymbolText = currency == null ? null : currency.SymbolText
            };

        return await candidates.FirstOrDefaultAsync(ct);
    }

    public async Task<TrialProductSelection> ApplyTrialDiscountsAsync(
        TrialProductSelection product,
        int seats,
        int? cartDiscountId = null,
        DateTime? currentDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var now = currentDateTime ?? await GetDatabaseCurrentDateTimeAsync(cancellationToken);
        var selectedCartDiscountId = cartDiscountId;
        var selectedCampaignDiscountId = (int?)null;
        var selectedTierDiscountId = (int?)null;
        var supplementalDiscountId = (int?)null;
        byte? discountTypeId = null;
        byte? discountMethodId = null;

        if (!selectedCartDiscountId.HasValue || selectedCartDiscountId == 0)
        {
            var campaignDiscount = await (
                from campaign in _db.MessageCampaign.AsNoTracking()
                join platform in _db.MessageCampaignPlatform.AsNoTracking()
                    on campaign.MessageCampaignId equals platform.MessageCampaignId
                join campaignDiscountLink in _db.MessageCampaignCartDiscount.AsNoTracking()
                    on campaign.MessageCampaignId equals campaignDiscountLink.MessageCampaignId
                join languageLocation in _db.MessageCampaignLanguageLocation.AsNoTracking()
                    on campaign.MessageCampaignId equals languageLocation.MessageCampaignId
                join item in _db.CartDiscountItem.AsNoTracking()
                    on campaignDiscountLink.CartDiscountId equals item.CartDiscountId
                join discount in _db.CartDiscount.AsNoTracking()
                    on item.CartDiscountId equals discount.CartDiscountId
                where languageLocation.LanguageCode.ToUpper() == product.LanguageCode
                    && languageLocation.LocationCode.ToUpper() == product.LocationCode
                    && item.LicenseSeats == 1
                    && item.LicenseCategoryId == product.LicenseCategoryId
                    && item.Years == product.Years
                    && item.CartDiscountMethodId == 3
                    && discount.CartDiscountTypeId == 1
                    && discount.CartDiscountStatusId == 1
                    && campaign.MessageCampaignEnabled == 1
                    && platform.MessagePlatformId == 8
                    && campaign.MessageCampaignClassId == 7
                    && campaign.MessageCampaignStartDate <= now
                    && campaign.MessageCampaignEndDate != null
                    && now < campaign.MessageCampaignEndDate
                orderby item.Discount descending
                select new { item.CartDiscountId, item.Discount })
                .FirstOrDefaultAsync(cancellationToken);

            if (campaignDiscount is not null)
            {
                selectedCartDiscountId = campaignDiscount.CartDiscountId;
            }
        }

        if (selectedCartDiscountId.HasValue && selectedCartDiscountId != 0)
        {
            discountTypeId = await _db.CartDiscount.AsNoTracking()
                .Where(discount => discount.CartDiscountId == selectedCartDiscountId)
                .Select(discount => (byte?)discount.CartDiscountTypeId)
                .FirstOrDefaultAsync(cancellationToken);

            discountMethodId = await _db.CartDiscountItem.AsNoTracking()
                .Where(item => item.CartDiscountId == selectedCartDiscountId)
                .Select(item => (byte?)item.CartDiscountMethodId)
                .MinAsync(cancellationToken);
        }

        if (discountTypeId.HasValue && discountTypeId != 4 &&
            discountMethodId.HasValue && discountMethodId != 2)
        {
            supplementalDiscountId = selectedCartDiscountId;
            selectedCampaignDiscountId = selectedCartDiscountId;
            selectedCartDiscountId = null;
        }

        if ((!selectedCartDiscountId.HasValue || selectedCartDiscountId == 0) &&
            (!discountMethodId.HasValue || discountMethodId != 2))
        {
            selectedTierDiscountId = await (
                from item in _db.CartDiscountItem.AsNoTracking()
                join discount in _db.CartDiscount.AsNoTracking()
                    on item.CartDiscountId equals discount.CartDiscountId
                where item.LowRange.HasValue
                    && item.LowRange <= seats
                    && item.HighRange.HasValue
                    && seats < item.HighRange
                    && item.ProductTypeId == product.ProductTypeId
                    && item.LicenseCategoryName == product.LicenseCategoryName
                    && item.CartDiscountMethodId == 3
                    && discount.CartDiscountStatusId == 1
                    && discount.CartDiscountTypeId == 4
                orderby item.Discount descending
                select (int?)item.CartDiscountId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        decimal? standardDiscount = 0m;
        if (discountMethodId != 2 && selectedTierDiscountId.HasValue)
        {
            var standardItem = await _db.CartDiscountItem.AsNoTracking()
                .Where(item => item.CartDiscountId == selectedTierDiscountId
                    && item.LowRange.HasValue
                    && item.LowRange <= seats
                    && item.HighRange.HasValue
                    && seats < item.HighRange
                    && item.ProductTypeId == product.ProductTypeId)
                .FirstOrDefaultAsync(cancellationToken);

            if (standardItem is not null)
            {
                standardDiscount = CalculateStandardDiscount(product, standardItem);
            }
        }

        decimal? supplementalDiscount = 0m;
        if (supplementalDiscountId.HasValue && discountMethodId != 2)
        {
            var supplementalItem = await _db.CartDiscountItem.AsNoTracking()
                .Where(item => item.CartDiscountId == supplementalDiscountId)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplementalItem?.CartDiscountMethodId == 3)
            {
                supplementalDiscount = CalculateSupplementalDiscount(product, standardDiscount, supplementalItem);
            }
        }

        decimal? unitRetailPrice = product.RetailPrice;
        if (discountMethodId == 2 && selectedCartDiscountId.HasValue)
        {
            var fixedPriceDiscount = await _db.CartDiscountItem.AsNoTracking()
                .Where(item => item.CartDiscountId == selectedCartDiscountId)
                .Select(item => (double?)item.Discount)
                .FirstOrDefaultAsync(cancellationToken);
            unitRetailPrice = fixedPriceDiscount.HasValue
                ? (decimal?)ToSqlMoney(fixedPriceDiscount.Value * product.Years)
                : null;
        }

        var extendedRetail = MultiplyMoney(unitRetailPrice, seats);
        var extendedStandard = MultiplyMoney(standardDiscount, seats);
        var extendedSupplemental = MultiplyMoney(supplementalDiscount, seats);
        decimal? netPrice = unitRetailPrice.HasValue && standardDiscount.HasValue && supplementalDiscount.HasValue
            ? MultiplyMoney(
                ToSqlMoney(unitRetailPrice.Value - ToSqlMoney(standardDiscount.Value + supplementalDiscount.Value)),
                seats)
            : null;

        return product with
        {
            RetailPrice = unitRetailPrice,
            StandardDiscountPerItem = standardDiscount,
            SupplementalDiscountPerItem = supplementalDiscount,
            AppliedTierDiscountId = selectedTierDiscountId,
            AppliedCampaignDiscountId = selectedCampaignDiscountId,
            ExtendedRetailPrice = extendedRetail,
            ExtendedStandardDiscount = extendedStandard,
            ExtendedSupplementalDiscount = extendedSupplemental,
            NetPrice = netPrice,
            UpgradePrice = netPrice,
            LegacyCartDiscountId = supplementalDiscountId
        };
    }

    private static decimal? CalculateStandardDiscount(
        TrialProductSelection product,
        CartDiscountItem item)
    {
        if (item.CartDiscountMethodId == 1)
        {
            if (!product.RetailPrice.HasValue)
                return null;

            var productSetRetailTotal = product.RetailPrice.Value;
            if (productSetRetailTotal == 0m)
                throw new DivideByZeroException("Legacy standard discount divides by total retail price.");

            var convertedDiscount = ToSqlMoney(item.Discount);
            var digits = product.CurrencyCode == "JPY" ? 0 : 2;
            return ToSqlMoney(RoundSql((double)(convertedDiscount *
                (product.RetailPrice.Value / productSetRetailTotal)), digits));
        }

        if (item.CartDiscountMethodId == 3 && product.CurrencyCode is not null)
        {
            if (!product.RetailPrice.HasValue)
                return null;

            var isZeroYearProduct = product.Years == 0d;
            var retailPerPeriod = isZeroYearProduct
                ? (double)product.RetailPrice.Value
                : (double)product.RetailPrice.Value / product.Years;
            var discountedPerPeriod = (1d - item.Discount / 100d) * retailPerPeriod;
            var digits = product.CurrencyCode == "JPY" ? 0 : 2;
            var roundedPeriodDiscount = RoundSql(retailPerPeriod - discountedPerPeriod, digits);
            return ToSqlMoney(isZeroYearProduct
                ? roundedPeriodDiscount
                : roundedPeriodDiscount * product.Years);
        }

        return 0m;
    }

    private static decimal? CalculateSupplementalDiscount(
        TrialProductSelection product,
        decimal? standardDiscount,
        CartDiscountItem item)
    {
        if (!product.RetailPrice.HasValue || !standardDiscount.HasValue)
            return null;

        var netBeforeSupplemental = product.RetailPrice.Value - standardDiscount.Value;
        var amount = (item.Discount / 100d) * (double)netBeforeSupplemental;
        return ToSqlMoney(RoundSql(amount, 2));
    }

    private async Task<DateTime> GetDatabaseCurrentDateTimeAsync(CancellationToken ct)
    {
        if (_db.Database.IsSqlServer())
        {
            return await _db.Database
                .SqlQuery<DateTime>($"SELECT GETDATE() AS [Value]")
                .SingleAsync(ct);
        }

        return DateTime.Now;
    }

    private static double RoundSql(double value, int digits)
        => Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private static decimal ToSqlMoney(double value)
        => decimal.Round(Convert.ToDecimal(value), 4, MidpointRounding.AwayFromZero);

    private static decimal ToSqlMoney(decimal value)
        => decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal? MultiplyMoney(decimal? value, int quantity)
        => value.HasValue ? ToSqlMoney(value.Value * quantity) : null;

    private static void AddParameters(
        DbCommand command,
        MultiTrialSubmissionRequest r,
        string trialRegistrationJson)
    {
        void Add(string name, SqlDbType type, int size, object? value)
        {
            var parameter = size > 0
                ? new SqlParameter(name, type, size)
                : new SqlParameter(name, type);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        Add("@first_name", SqlDbType.NVarChar, 225, r.FirstName);
        Add("@last_name", SqlDbType.NVarChar, 225, r.LastName);
        Add("@company_name", SqlDbType.NVarChar, 255, r.CompanyName);
        Add("@company_type_name", SqlDbType.VarChar, 50, r.CompanyTypeName);
        Add("@phone_number", SqlDbType.VarChar, 64, r.PhoneNumber);
        Add("@customer_email", SqlDbType.VarChar, 100, r.CustomerEmail);
        Add("@address_1", SqlDbType.NVarChar, 255, r.Address1);
        Add("@address_2", SqlDbType.NVarChar, 255, r.Address2);
        Add("@city", SqlDbType.NVarChar, 130, r.City);
        Add("@state", SqlDbType.NVarChar, 2, r.State);
        Add("@postal_code", SqlDbType.NVarChar, 32, r.PostalCode);
        Add("@country", SqlDbType.VarChar, 75, r.Country);
        Add("@language_code", SqlDbType.VarChar, 2, r.LanguageCode);
        Add("@location_code", SqlDbType.VarChar, 3, r.LocationCode);
        Add("@license_category_name", SqlDbType.VarChar, 10, r.LicenseCategoryName);
        Add("@license_seats", SqlDbType.Int, 0, r.LicenseSeats);
        Add("@years", SqlDbType.Float, 0, r.Years);
        Add("@trial_days", SqlDbType.Int, 0, r.TrialDays);
        Add("@license_keycode_type_id", SqlDbType.Int, 0, r.LicenseKeycodeTypeId);
        Add("@partner_key", SqlDbType.VarChar, 36,
            string.IsNullOrWhiteSpace(r.PartnerKey) ? null : r.PartnerKey);
        Add("@account_user_name", SqlDbType.VarChar, 100, r.AccountUserName);
        Add("@partner_account_code", SqlDbType.VarChar, 50, r.PartnerAccountCode);
        Add("@sfdc_lead_id", SqlDbType.VarChar, 18, r.SfdcLeadId);
        Add("@sfdc_opportunity_id", SqlDbType.VarChar, 18, r.SfdcOpportunityId);
        Add("@sfdc_trial_id", SqlDbType.VarChar, 18, r.SfdcTrialId);
        Add("@salesforce_campaign_id", SqlDbType.VarChar, 18, r.SalesforceCampaignId);
        Add("@partner_product_id", SqlDbType.VarChar, 20, r.PartnerProductId);
        Add("@sfdc_distributor_id", SqlDbType.VarChar, 18, r.SfdcDistributorId);
        Add("@sfdc_reseller_id", SqlDbType.VarChar, 18, r.SfdcResellerId);
        Add("@license_distribution_method_code", SqlDbType.Char, 4, r.LicenseDistributionMethodCode);
        Add("@salesforce_license_id", SqlDbType.VarChar, 18, r.SalesforceLicenseId);
        Add("@trial_registration_json", SqlDbType.VarChar, -1, trialRegistrationJson);
        Add("@opt_in", SqlDbType.Int, 0, r.OptIn);
    }

    private static TrialRegistrationProcedureResult Map(DbDataReader reader)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        return new TrialRegistrationProcedureResult
        {
            ResponseCode = GetInt(reader, columns, "response_code"),
            Message = GetString(reader, columns, "message"),
            TrialRegistrationId = GetInt(reader, columns, "trial_registration_id"),
            RequestType = GetString(reader, columns, "request_type"),
            FirstName = GetString(reader, columns, "first_name"),
            LastName = GetString(reader, columns, "last_name"),
            CustomerEmail = GetString(reader, columns, "customer_email"),
            ProductLineId = GetInt(reader, columns, "product_line_id"),
            LicenseCategoryName = GetString(reader, columns, "license_category_name"),
            LicenseCategoryId = GetInt(reader, columns, "license_category_id"),
            LicenseModuleList = GetString(reader, columns, "license_module_list"),
            LicenseType = GetString(reader, columns, "license_type"),
            LicenseTypeId = GetInt(reader, columns, "license_type_id"),
            CapabilityActivationDays = GetInt(reader, columns, "capability_activation_days"),
            LicenseSeats = GetInt(reader, columns, "license_seats"),
            StorageGb = GetInt(reader, columns, "storage_gb"),
            LicenseDistributionMethodCode = GetString(reader, columns, "license_distribution_method_code"),
        };
    }

    private static int? GetInt(DbDataReader reader, HashSet<string> columns, string name)
    {
        var value = GetValue(reader, columns, name);
        return value is null ? null : Convert.ToInt32(value);
    }

    private static string? GetString(DbDataReader reader, HashSet<string> columns, string name)
        => GetValue(reader, columns, name)?.ToString();

    private static object? GetValue(DbDataReader reader, HashSet<string> columns, string name)
    {
        if (!columns.Contains(name)) return null;
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }
}
