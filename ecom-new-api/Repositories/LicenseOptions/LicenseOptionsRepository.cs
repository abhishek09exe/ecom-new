using System.Data;
using System.Data.Common;
using System.Text.Json;
using ecom_new_api.Data;
using ecom_new_api.Models.Responses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories.LicenseOptions;

public sealed class LicenseOptionsRepository : ILicenseOptionsRepository
{
   private readonly AppDbContext _db;

   public LicenseOptionsRepository(AppDbContext db) => _db = db;

   private async Task<List<Dictionary<string, object?>>> ExecToDictionaryAsync(
       string storedProcedure,
       IEnumerable<DbParameter> parameters,
       CancellationToken ct)
   {
       var results = new List<Dictionary<string, object?>>();
       var conn = _db.Database.GetDbConnection();
       var shouldClose = false;
       if (conn.State != ConnectionState.Open)
       {
           await conn.OpenAsync(ct);
           shouldClose = true;
       }

       await using (var cmd = conn.CreateCommand())
       {
           cmd.CommandText = storedProcedure;
           cmd.CommandType = CommandType.StoredProcedure;
           foreach (var p in parameters)
           {
               var clone = cmd.CreateParameter();
               clone.ParameterName = p.ParameterName;
               clone.Value = p.Value ?? DBNull.Value;
               clone.DbType = p.DbType;
               cmd.Parameters.Add(clone);
           }

           await using var reader = await cmd.ExecuteReaderAsync(ct);
           while (await reader.ReadAsync(ct))
           {
               var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
               for (int i = 0; i < reader.FieldCount; i++)
               {
                   var name = reader.GetName(i);
                   row[name] = await reader.IsDBNullAsync(i, ct) ? null : reader.GetValue(i);
               }
               results.Add(row);
           }
       }

       if (shouldClose)
           await conn.CloseAsync();

       return results;
   }

   public async Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
       string messageKey,
       CancellationToken ct = default)
   {
       if (string.IsNullOrWhiteSpace(messageKey))
           return null;

       var normalizedLocale = "en_US";

       var messageKeyParam = new SqlParameter("@message_key", messageKey);
       var messageKeyRows = await ExecToDictionaryAsync(
           "usp_cart_select_message_key",
           new[] { messageKeyParam },
           ct);

       var messageKeyRow = messageKeyRows.FirstOrDefault();
       if (messageKeyRow is null || !messageKeyRow.TryGetValue("message_key_json", out var messageJsonObj))
           return null;

       var messagePayload = ParseJsonObject(messageJsonObj?.ToString());
       if (messagePayload is null)
           return null;

       var licenseId = TryGetInt(messagePayload, "license_id");
       if (!licenseId.HasValue)
           return null;

       var licenseIdParam = new SqlParameter("@license_id", licenseId.Value);
       var licenseRows = await ExecToDictionaryAsync(
           "usp_license_select_license_by_id",
           new[] { licenseIdParam },
           ct);

       var license = licenseRows.FirstOrDefault() ?? new Dictionary<string, object?>();
       var verified = license.Count > 0;

       var resolvedMessageKey = messageKey;
       if (license.TryGetValue("license_key", out var licenseKeyValue) && licenseKeyValue is not null)
       {
           resolvedMessageKey = licenseKeyValue.ToString() ?? messageKey;
       }

       var profileParam = new SqlParameter("@license_id", licenseId.Value);
       var profileRows = await ExecToDictionaryAsync(
           "usp_cart_select_license_profile",
           new[] { profileParam },
           ct);

       var profile = profileRows
           .Where(r => r.TryGetValue("license_category_name", out var n) && n is not null)
           .ToDictionary(
               r => r["license_category_name"]!.ToString()!,
               r => r,
               StringComparer.OrdinalIgnoreCase);

       string? primaryCategoryName = null;
       if (license.TryGetValue("license_category_name", out var primaryCat) && primaryCat is not null)
           primaryCategoryName = primaryCat.ToString();

       if (string.IsNullOrWhiteSpace(primaryCategoryName) && profile.Count > 0)
       {
           primaryCategoryName = profile.Values
               .FirstOrDefault(v => TryGetInt(v, "item_hierarchy_id") == 1)
               ?.GetValueOrDefault("license_category_name")?.ToString();
       }

       var upgradeCategories = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
       if (!string.IsNullOrWhiteSpace(primaryCategoryName))
       {
           foreach (var hierarchyId in new[] { 1, 2 })
           {
               var categoryParam = new SqlParameter("@license_category_name", primaryCategoryName);
               var localeParam = new SqlParameter("@locale", normalizedLocale);
               var hierarchyParam = new SqlParameter("@item_hierarchy_id", hierarchyId);

               var upgradeRows = await ExecToDictionaryAsync(
                   "usp_product_select_license_category_upgrade",
                   new DbParameter[] { categoryParam, localeParam, hierarchyParam },
                   ct);

               foreach (var row in upgradeRows)
               {
                   if (!row.TryGetValue("upgrade_license_category_name", out var upgradeNameObj) || upgradeNameObj is null)
                       continue;

                   var upgradeName = upgradeNameObj.ToString();
                   if (string.IsNullOrWhiteSpace(upgradeName))
                       continue;

                   upgradeCategories[upgradeName] = row;
               }
           }
       }

       var billingLocaleParam = new SqlParameter("@locale", normalizedLocale);
       var billingLicenseParam = new SqlParameter("@license_id", licenseId.Value);
       var billingRows = await ExecToDictionaryAsync(
           "usp_cart_select_license_billing_model",
           new[] { billingLocaleParam, billingLicenseParam },
           ct);

       var billingModelMap = new Dictionary<string, Dictionary<string, object?>>();
       foreach (var row in billingRows)
       {
           if (!row.TryGetValue("license_attribute_license_value", out var valueObj) || valueObj is null)
               continue;

           var key = Convert.ToString(valueObj, System.Globalization.CultureInfo.InvariantCulture);
           if (string.IsNullOrWhiteSpace(key))
               continue;

           billingModelMap[key] = row;
       }

       var mappedLicense = MapLicense(license);
       var mappedProfile = profile.ToDictionary(
           kvp => kvp.Key,
           kvp => MapLicenseProfileEntry(kvp.Value),
           StringComparer.OrdinalIgnoreCase);
       var mappedUpgradeCategories = upgradeCategories.ToDictionary(
           kvp => kvp.Key,
           kvp => MapUpgradeCategory(kvp.Value),
           StringComparer.OrdinalIgnoreCase);
       var mappedBillingModels = billingModelMap.Values
           .Select(MapBillingModel)
           .ToList();

       var siteId = ExtractSiteId(license) ?? ExtractSiteId(profile.Values.FirstOrDefault());
       mappedProfile.TryGetValue(primaryCategoryName ?? string.Empty, out var primaryProfile);

       return new LicenseOptionsResponse
       {
           Keycode = mappedLicense.Keycode ?? messageKey,
           LicenseKey = mappedLicense.LicenseKey,
           LicenseStatus = TryGetString(license, "license_status_description") ?? TryGetString(license, "license_status"),
           ProductLine = mappedLicense.ProductLineDescription,
           LicenseCategory = primaryCategoryName,
           LicenseCategoryDescription = mappedLicense.LicenseCategoryDescription ?? primaryProfile?.LicenseCategoryDescription,
           LicenseSeats = mappedLicense.LicenseSeats,
           ExpirationDate = mappedLicense.LicenseExpirationDate ?? mappedLicense.EndDate,
           License = mappedLicense,
           LicenseVerified = verified,
           LicenseProfile = mappedProfile,
           LicenseSiteId = siteId,
           UpgradeCategories = mappedUpgradeCategories,
           BillingModels = mappedBillingModels
       };
   }

   private static string? ExtractSiteId(Dictionary<string, object?>? row)
   {
       if (row is null || row.Count == 0)
           return null;

       if (row.TryGetValue("license_site_id", out var siteId))
           return siteId?.ToString();

       if (row.TryGetValue("site_id", out var altSiteId))
           return altSiteId?.ToString();

       return null;
   }

   private static LicenseInfoResponse MapLicense(Dictionary<string, object?> row)
       => new()
       {
           Keycode = TryGetString(row, "keycode"),
           ProductLineDescription = TryGetString(row, "product_line_description"),
           LicenseStatusId = TryGetInt(row, "license_status_id"),
           LicenseTypeDescription = TryGetString(row, "license_type_description"),
           LicenseKeycodeTypeId = TryGetInt(row, "license_keycode_type_id"),
           MaxDailyActivations = TryGetInt(row, "max_daily_activations"),
           LicenseExpirationDate = TryGetDateTime(row, "license_expiration_date"),
           ParentKeycode = TryGetString(row, "parent_keycode"),
           LicenseKey = TryGetString(row, "license_key"),
           LicenseSeats = TryGetInt(row, "license_seats"),
           ConsumedSeats = TryGetInt(row, "consumed_seats"),
           SeatsUsed = TryGetInt(row, "seats_used"),
           StorageGb = TryGetInt(row, "storage_gb"),
           LicenseCategoryName = TryGetString(row, "license_category_name"),
           LicenseCategoryDescription = TryGetString(row, "license_category_description"),
           StartDate = TryGetDateTime(row, "start_date"),
           EndDate = TryGetDateTime(row, "end_date"),
           DaysRemaining = TryGetInt(row, "days_remaining"),
           IsExpired = TryGetBool(row, "is_expired"),
           LicenseAttributeDescription = TryGetString(row, "license_attribute_description"),
           LicenseAttributeTag = TryGetString(row, "license_attribute_tag"),
           LicenseAttributeLicenseValue = TryGetInt(row, "license_attribute_license_value"),
           LicenseAttributeLicenseValueDescription = TryGetString(row, "license_attribute_license_value_description"),
           LicenseAttributeLastModified = TryGetDateTime(row, "license_attribute_last_modified"),
           OemType = TryGetString(row, "oem_type"),
           PortalFlag = TryGetInt(row, "portal_flag"),
           RenewalCount = TryGetInt(row, "renewal_count"),
           LicenseOriginChannelName = TryGetString(row, "license_origin_channel_name"),
           LicenseOriginalActivationDate = TryGetDateTime(row, "license_original_activation_date"),
           EmailOptIn = TryGetInt(row, "email_opt_in"),
           LicenseDistributionMethodCode = TryGetString(row, "license_distribution_method_code"),
           NextBillDate = TryGetDateTime(row, "next_bill_date"),
           CapabilityTypeDescription = TryGetString(row, "capability_type_description"),
       };

   private static LicenseProfileEntryResponse MapLicenseProfileEntry(Dictionary<string, object?> row)
       => new()
       {
           LicenseCategoryName = TryGetString(row, "license_category_name"),
           LicenseCategoryDescription = TryGetString(row, "license_category_description"),
           LicenseCategoryId = TryGetInt(row, "license_category_id"),
           LicenseKeycodeTypeId = TryGetInt(row, "license_keycode_type_id"),
           CategoryTypeName = TryGetString(row, "category_type_name"),
           LicenseStatusId = TryGetInt(row, "license_status_id"),
           LicenseStatusDescription = TryGetString(row, "license_status_description"),
           StartDate = TryGetDateTime(row, "start_date"),
           ExpirationDate = TryGetDateTime(row, "expiration_date"),
           LicenseSeats = TryGetInt(row, "license_seats"),
           StorageGb = TryGetInt(row, "storage_gb"),
           LicenseAttributeId = TryGetInt(row, "license_attribute_id"),
           LicenseAttributeDescription = TryGetString(row, "license_attribute_description"),
           LicenseAttributeLicenseValue = TryGetInt(row, "license_attribute_license_value"),
           LicenseAttributeLicenseValueDescription = TryGetString(row, "license_attribute_license_value_description"),
           ItemHierarchyId = TryGetInt(row, "item_hierarchy_id"),
           ItemHierarchyName = TryGetString(row, "item_hierarchy_name"),
           AutorenewalCycleName = TryGetString(row, "autorenewal_cycle_name"),
           AutorenewalCycle = TryGetDecimal(row, "autorenewal_cycle"),
           UsagePricingModelId = TryGetInt(row, "usage_pricing_model_id"),
           UsagePricingModelName = TryGetString(row, "usage_pricing_model_name"),
           RetentionModelId = TryGetInt(row, "retention_model_id"),
           RetentionModelName = TryGetString(row, "retention_model_name"),
           RetentionTerm = TryGetInt(row, "retention_term"),
           RetentionModelTypeId = TryGetInt(row, "retention_model_type_id"),
           ProductPlatformId = TryGetInt(row, "product_platform_id"),
           ProductPlatformName = TryGetString(row, "product_platform_name"),
           LicenseAutorenewalValue = TryGetInt(row, "license_autorenewal_value"),
           ProductPricingLevelId = TryGetInt(row, "product_pricing_level_id"),
           PricingLevel = TryGetString(row, "pricing_level"),
           PricingLevelDescription = TryGetString(row, "pricing_level_description"),
           LicenseVaultJson = TryGetString(row, "license_vault_json"),
           MostRecentOrderTerm = TryGetDouble(row, "most_recent_order_term"),
       };

   private static UpgradeCategoryResponse MapUpgradeCategory(Dictionary<string, object?> row)
       => new()
       {
           LicenseCategoryName = TryGetString(row, "license_category_name"),
           UpgradeLicenseCategoryName = TryGetString(row, "upgrade_license_category_name"),
           ItemHierarchyId = TryGetInt(row, "item_hierarchy_id"),
           ItemHierarchyName = TryGetString(row, "item_hierarchy_name"),
       };

   private static BillingModelResponse MapBillingModel(Dictionary<string, object?> row)
       => new()
       {
           ProductTypeId = TryGetInt(row, "product_type_id"),
           ProductTypeDescription = TryGetString(row, "product_type_description"),
           LicenseAttributeDescription = TryGetString(row, "license_attribute_description"),
           LicenseAttributeLicenseValue = TryGetInt(row, "license_attribute_license_value"),
           LicenseAttributeLicenseValueDescription = TryGetString(row, "license_attribute_license_value_description"),
       };

   private static Dictionary<string, object?>? ParseJsonObject(string? json)
   {
       if (string.IsNullOrWhiteSpace(json))
           return null;

       try
       {
           using var doc = JsonDocument.Parse(json);
           var dict = new Dictionary<string, object?>();
           foreach (var prop in doc.RootElement.EnumerateObject())
           {
               dict[prop.Name] = prop.Value.GetRawText();
           }
           return dict;
       }
       catch
       {
           return null;
       }
   }

   private static int? TryGetInt(Dictionary<string, object?> row, string key)
   {
       if (!row.TryGetValue(key, out var value) || value is null)
           return null;

       if (value is int intValue)
           return intValue;

       if (int.TryParse(value.ToString(), out var parsed))
           return parsed;

       return null;
   }

   private static string? TryGetString(Dictionary<string, object?> row, string key)
   {
       if (!row.TryGetValue(key, out var value) || value is null)
           return null;

       return value.ToString();
   }

   private static bool TryGetBool(Dictionary<string, object?> row, string key)
   {
       if (!row.TryGetValue(key, out var value) || value is null)
           return false;

       if (value is bool boolValue)
           return boolValue;

       if (bool.TryParse(value.ToString(), out var parsedBool))
           return parsedBool;

       if (int.TryParse(value.ToString(), out var parsedInt))
           return parsedInt != 0;

       return false;
   }

   private static DateTime? TryGetDateTime(Dictionary<string, object?> row, string key)
   {
       if (!row.TryGetValue(key, out var value) || value is null)
           return null;

       if (value is DateTime dateTimeValue)
           return dateTimeValue;

       if (DateTime.TryParse(value.ToString(), out var parsed))
           return parsed;

       return null;
   }

   private static decimal? TryGetDecimal(Dictionary<string, object?> row, string key)
   {
       if (!row.TryGetValue(key, out var value) || value is null)
           return null;

       if (value is decimal decimalValue)
           return decimalValue;

       if (decimal.TryParse(value.ToString(), out var parsed))
           return parsed;

       return null;
   }

   private static double? TryGetDouble(Dictionary<string, object?> row, string key)
   {
       if (!row.TryGetValue(key, out var value) || value is null)
           return null;

       if (value is double doubleValue)
           return doubleValue;

       if (double.TryParse(value.ToString(), out var parsed))
           return parsed;

       return null;
   }

    public async Task<string?> ResolveKeycodeFromMessageKeyAsync(
        string messageKey,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(messageKey, out var guid)) return null;

        return await (
            from lk in _db.LicenseKey
            join l in _db.License on lk.LicenseId equals l.LicenseId
            where lk.Key == guid
            select l.Keycode
        ).FirstOrDefaultAsync(ct);
    }
}
