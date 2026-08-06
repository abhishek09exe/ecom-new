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

       var siteId = ExtractSiteId(license) ?? ExtractSiteId(profile.Values.FirstOrDefault());

       return new LicenseOptionsResponse
       {
           License = license,
           LicenseVerified = verified,
           LicenseProfile = profile,
           LicenseSiteId = siteId,
           UpgradeCategories = upgradeCategories,
           BillingModels = billingModelMap
       };
   }

   private static object? ExtractSiteId(Dictionary<string, object?>? row)
   {
       if (row is null || row.Count == 0)
           return null;

       if (row.TryGetValue("license_site_id", out var siteId))
           return siteId;

       if (row.TryGetValue("site_id", out var altSiteId))
           return altSiteId;

       return null;
   }

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
