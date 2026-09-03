using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text.Json;
using ecom_new_api.Data;
using ecom_new_api.Models.Requests;
using ecom_new_api.Observability;
using Microsoft.Data.SqlClient;
using Prometheus;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Services.Pricing;

// ── SP result POCOs ──────────────────────────────────────────────────────────────

public class MessageKeyResult
{
    [Column("message_key_type")]
    public string? MessageKeyType { get; set; }
    [Column("message_key_json")]
    public string? MessageKeyJson { get; set; }
}

public class MessageKeyJson
{
    public int? CartDiscountId { get; set; }
    public string? Keycode { get; set; }
    public int? LicenseId { get; set; }
    public int? CustomerId { get; set; }
}

public class CampaignDiscountResult
{
    [Column("cart_discount_id")]
    public int CartDiscountId { get; set; }
    [Column("cart_discount_key")]
    public string? CartDiscountKey { get; set; }
    [Column("cart_discount_description")]
    public string? CartDiscountDescription { get; set; }
    [Column("license_category_name")]
    public string? LicenseCategoryName { get; set; }
}

public class SiteDiscountResult
{
    [Column("cart_discount_id")]
    public int CartDiscountId { get; set; }
}

public class CartDiscountItemResult
{
    [Column("cart_discount_item_id")]
    public int CartDiscountItemId { get; set; }
    [Column("cart_discount_id")]
    public int CartDiscountId { get; set; }
    [Column("cart_discount_method_id")]
    public int? CartDiscountMethodId { get; set; }
    [Column("discount")]
    public double? Discount { get; set; }
    [Column("license_category_name")]
    public string? LicenseCategoryName { get; set; }
    [Column("license_seats")]
    public int? LicenseSeats { get; set; }
    [Column("years")]
    public double? Years { get; set; }
}

public class CartDiscountResult
{
    [Column("cart_discount_id")]
    public int CartDiscountId { get; set; }
    [Column("cart_discount_key")]
    public string? CartDiscountKey { get; set; }
    [Column("cart_discount_description")]
    public string? CartDiscountDescription { get; set; }
}

public class LicenseCampaignResult
{
    [Column("message_campaign_name")]
    public string? MessageCampaignName { get; set; }
}

// ── Context returned by ResolveAsync ────────────────────────────────────────────

public class ResolvedBundleContext
{
    public BundlePricingItem Bundle { get; set; } = null!;
    public string? Keycode { get; set; }
    public int? CartDiscountId { get; set; }
    public string? MessageCampaignName { get; set; }
    public bool IncludeMessageKeyInBundle { get; set; } = false;
}

// ── Service ──────────────────────────────────────────────────────────────────────

public class MessageKeyService
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<MessageKeyService> _logger;

    public MessageKeyService(AppDbContext ctx, ILogger<MessageKeyService> logger)
        => (_ctx, _logger) = (ctx, logger);

    public async Task<ResolvedBundleContext> ResolveAsync(BundlePricingItem bundle, string locale)
    {
        var ctx = new ResolvedBundleContext { Bundle = bundle };
        var (lang, iso3) = ParseLocale(locale);

        // Numeric message_key → could be zuora_campaign_id
        if (!string.IsNullOrEmpty(bundle.MessageKey) && int.TryParse(bundle.MessageKey, out _))
        {
            var r = await ClassifyKeyAsync(bundle);
            if (r?.MessageKeyType == "zuora_campaign_id")
            {
                ctx.IncludeMessageKeyInBundle = true;
                return ctx;
            }
            ctx.Bundle = CloneWithoutMessageKey(bundle);
            return ctx;
        }

        // UUID message_key
        if (!string.IsNullOrEmpty(bundle.MessageKey) && Guid.TryParse(bundle.MessageKey, out _))
        {
            var r = await ClassifyKeyAsync(bundle);

            if (r?.MessageKeyType is "license_key" or "zuora_license_key")
            {
                ctx.IncludeMessageKeyInBundle = true;
                ctx.Keycode             = await GetKeycodeAsync(bundle.MessageKey);
                ctx.MessageCampaignName = await GetCampaignNameAsync(ctx.Keycode);
                return ctx;
            }

            if (r?.MessageKeyType == "cart_discount_key")
            {
                var j = r.MessageKeyJson != null
                    ? JsonSerializer.Deserialize<MessageKeyJson>(r.MessageKeyJson, JsonOpts)
                    : null;
                if (j?.CartDiscountId != null && await VerifyDiscountAsync(j.CartDiscountId.Value, bundle))
                {
                    ctx.CartDiscountId = j.CartDiscountId;
                    return ctx;
                }
            }

            // Campaign-based discount: specific then generic
            var disc = await GetDiscountByCampaignAsync(bundle.MessageKey, bundle)
                    ?? await GetDiscountByCampaignAsync(bundle.MessageKey, null);
            if (disc != null)
            {
                var key = await GetDiscountKeyAsync(disc.CartDiscountId, bundle);
                if (key != null)
                {
                    ctx.CartDiscountId = disc.CartDiscountId;
                    return ctx;
                }
            }
        }
        else
        {
            ctx.Bundle = CloneWithoutMessageKey(bundle);
        }

        // Site-level fallback discount
        var site = await GetSiteDiscountAsync(bundle, lang, iso3);
        if (site != null) ctx.CartDiscountId = site.CartDiscountId;
        return ctx;
    }

    // ── SP helpers ───────────────────────────────────────────────────────────────

    private async Task<MessageKeyResult?> ClassifyKeyAsync(BundlePricingItem bundle)
    {
        const string procName = "usp_cart_select_message_key";
        var p1 = new SqlParameter("@message_key",            SqlDbType.VarChar, 36)  { Value = bundle.MessageKey ?? (object)DBNull.Value };
        var p2 = new SqlParameter("@license_category_name",  SqlDbType.VarChar, 20)  { Value = (object?)bundle.LicenseCategoryName ?? DBNull.Value };
        var p3 = new SqlParameter("@years",                  SqlDbType.Int)           { Value = (object?)(int?)bundle.Years          ?? DBNull.Value };
        var p4 = new SqlParameter("@seats",                  SqlDbType.Int)           { Value = bundle.LicenseSeats };

        using var timer = AppMetrics.DbProcDuration.WithLabels(procName).NewTimer();
        try
        {
            var result = (await _ctx.Database
                .SqlQueryRaw<MessageKeyResult>(
                    "EXEC usp_cart_select_message_key @message_key, @license_category_name, @years, @seats",
                    p1, p2, p3, p4)
                .ToListAsync()).FirstOrDefault();
            AppMetrics.DbProcCalls.WithLabels(procName, "success").Inc();
            return result;
        }
        catch
        {
            AppMetrics.DbProcCalls.WithLabels(procName, "error").Inc();
            throw;
        }
    }

    private async Task<string?> GetKeycodeAsync(string? messageKey)
    {
        if (string.IsNullOrEmpty(messageKey)) return null;
        const string procName = "usp_cart_select_message_key";
        var p = new SqlParameter("@message_key", SqlDbType.VarChar, 36) { Value = messageKey };

        MessageKeyResult? r;
        using (var timer = AppMetrics.DbProcDuration.WithLabels(procName).NewTimer())
        {
            try
            {
                r = (await _ctx.Database
                    .SqlQueryRaw<MessageKeyResult>(
                        "EXEC usp_cart_select_message_key @message_key",
                        p)
                    .ToListAsync()).FirstOrDefault();
                AppMetrics.DbProcCalls.WithLabels(procName, "success").Inc();
            }
            catch
            {
                AppMetrics.DbProcCalls.WithLabels(procName, "error").Inc();
                throw;
            }
        }

        if (r?.MessageKeyJson == null) return null;
        var j = JsonSerializer.Deserialize<MessageKeyJson>(r.MessageKeyJson, JsonOpts);
        return j?.Keycode;
    }

    private async Task<string?> GetCampaignNameAsync(string? keycode)
    {
        if (string.IsNullOrEmpty(keycode)) return null;
        const string procName = "usp_cart_select_license_campaign";
        using var timer = AppMetrics.DbProcDuration.WithLabels(procName).NewTimer();
        try
        {
            var p = new SqlParameter("@keycode", SqlDbType.VarChar, 40) { Value = keycode };
            var r = (await _ctx.Database
                .SqlQueryRaw<LicenseCampaignResult>(
                    "EXEC usp_cart_select_license_campaign @keycode",
                    p)
                .ToListAsync()).FirstOrDefault();
            AppMetrics.DbProcCalls.WithLabels(procName, "success").Inc();
            return r?.MessageCampaignName;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            // SP not available in this environment — campaign name is optional metadata only
            AppMetrics.DbProcCalls.WithLabels(procName, "error").Inc();
            _logger.LogWarning(ex, "usp_cart_select_license_campaign unavailable for keycode={Keycode}; skipping campaign name", keycode);
            return null;
        }
    }

    private async Task<bool> VerifyDiscountAsync(int cartDiscountId, BundlePricingItem bundle)
    {
        const string procName = "usp_cart_select_cart_discount_item";
        using var timer = AppMetrics.DbProcDuration.WithLabels(procName).NewTimer();
        try
        {
            var p = new SqlParameter("@cart_discount_id", SqlDbType.Int) { Value = cartDiscountId };
            var items = await _ctx.Database
                .SqlQueryRaw<CartDiscountItemResult>(
                    "EXEC usp_cart_select_cart_discount_item @cart_discount_id",
                    p)
                .ToListAsync();
            AppMetrics.DbProcCalls.WithLabels(procName, "success").Inc();

            return items.Any(i =>
                (i.LicenseCategoryName == null || i.LicenseCategoryName == bundle.LicenseCategoryName) &&
                (i.LicenseSeats        == null || i.LicenseSeats        == bundle.LicenseSeats) &&
                (i.Years               == null || Math.Abs((i.Years.Value - (double)bundle.Years)) < 0.001));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            AppMetrics.DbProcCalls.WithLabels(procName, "error").Inc();
            _logger.LogWarning(ex, "usp_cart_select_cart_discount_item failed for cart_discount_id={CartDiscountId}", cartDiscountId);
            return false;
        }
    }

    private async Task<CampaignDiscountResult?> GetDiscountByCampaignAsync(
        string messageKey, BundlePricingItem? bundle)
    {
        const string procName = "usp_message_select_message_campaign_cart_discount";
        using var timer = AppMetrics.DbProcDuration.WithLabels(procName).NewTimer();
        try
        {
            var p1 = new SqlParameter("@message_campaign_key",    SqlDbType.UniqueIdentifier) { Value = Guid.Parse(messageKey) };
            var p2 = new SqlParameter("@license_category_name",   SqlDbType.VarChar, 10)      { Value = (object?)bundle?.LicenseCategoryName ?? DBNull.Value };
            var p3 = new SqlParameter("@license_seats",           SqlDbType.Int)               { Value = (object?)bundle?.LicenseSeats         ?? DBNull.Value };

            var result = (await _ctx.Database
                .SqlQueryRaw<CampaignDiscountResult>(
                    "EXEC usp_message_select_message_campaign_cart_discount @message_campaign_key, @license_category_name, @license_seats",
                    p1, p2, p3)
                .ToListAsync()).FirstOrDefault();
            AppMetrics.DbProcCalls.WithLabels(procName, "success").Inc();
            return result;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            AppMetrics.DbProcCalls.WithLabels(procName, "error").Inc();
            _logger.LogWarning(ex, "usp_message_select_message_campaign_cart_discount failed for message_key={MessageKey}", messageKey);
            return null;
        }
    }

    private async Task<CartDiscountResult?> GetDiscountKeyAsync(int cartDiscountId, BundlePricingItem bundle)
    {
        const string procName = "usp_cart_select_cart_discount";
        using var timer = AppMetrics.DbProcDuration.WithLabels(procName).NewTimer();
        try
        {
            var p = new SqlParameter("@cart_discount_id", SqlDbType.Int) { Value = cartDiscountId };
            var result = (await _ctx.Database
                .SqlQueryRaw<CartDiscountResult>(
                    "EXEC usp_cart_select_cart_discount @cart_discount_id",
                    p)
                .ToListAsync()).FirstOrDefault();
            AppMetrics.DbProcCalls.WithLabels(procName, "success").Inc();
            return result;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            AppMetrics.DbProcCalls.WithLabels(procName, "error").Inc();
            _logger.LogWarning(ex, "usp_cart_select_cart_discount failed for cart_discount_id={CartDiscountId}", cartDiscountId);
            return null;
        }
    }

    private async Task<CampaignDiscountResult?> GetSiteDiscountAsync(
        BundlePricingItem bundle, string lang, string iso3)
    {
        const string procName = "usp_cart_select_new_product_discount";
        using var timer = AppMetrics.DbProcDuration.WithLabels(procName).NewTimer();
        try
        {
            var p1 = new SqlParameter("@license_category_name",   SqlDbType.VarChar, 10) { Value = bundle.LicenseCategoryName };
            var p2 = new SqlParameter("@license_seats",           SqlDbType.Int)          { Value = bundle.LicenseSeats };
            var p3 = new SqlParameter("@years",                   SqlDbType.Float)        { Value = (double)bundle.Years };
            var p4 = new SqlParameter("@language_code",           SqlDbType.VarChar, 2)   { Value = lang };
            var p5 = new SqlParameter("@location_code",           SqlDbType.VarChar, 3)   { Value = iso3 };
            var p6 = new SqlParameter("@cart_discount_method_id", SqlDbType.TinyInt)      { Value = DBNull.Value };
            var p7 = new SqlParameter("@discount",                SqlDbType.Float)        { Value = DBNull.Value };

            var row = (await _ctx.Database
                .SqlQueryRaw<SiteDiscountResult>(
                    "EXEC usp_cart_select_new_product_discount @license_category_name, @license_seats, NULL, @years, @cart_discount_method_id, @discount, @language_code, @location_code",
                    p1, p2, p3, p4, p5, p6, p7)
                .ToListAsync()).FirstOrDefault();
            AppMetrics.DbProcCalls.WithLabels(procName, "success").Inc();

            if (row == null) return null;
            return new CampaignDiscountResult
            {
                CartDiscountId          = row.CartDiscountId,
                CartDiscountKey         = null,
                CartDiscountDescription = null,
                LicenseCategoryName     = null
            };
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            AppMetrics.DbProcCalls.WithLabels(procName, "error").Inc();
            _logger.LogWarning(ex, "usp_cart_select_new_product_discount failed for category={Category} lang={Lang} loc={Iso3}", bundle.LicenseCategoryName, lang, iso3);
            return null;
        }
    }

    // ── Utilities ────────────────────────────────────────────────────────────────

    private static (string lang, string iso3) ParseLocale(string locale)
    {
        var parts = locale.Replace("-", "_").Split('_');
        var lang  = parts[0].ToLower();
        var iso2  = parts.Length > 1 ? parts[1].ToUpper() : "US";
        var iso3  = iso2 switch
        {
            "US" => "USA", "GB" => "GBR", "CA" => "CAN", "AU" => "AUS",
            "DE" => "DEU", "FR" => "FRA", "JP" => "JPN", "NL" => "NLD",
            _    => iso2.PadRight(3, 'A')
        };
        return (lang, iso3);
    }

    private static BundlePricingItem CloneWithoutMessageKey(BundlePricingItem src)
        => new()
        {
            LicenseCategoryName          = src.LicenseCategoryName,
            LicenseSeats                 = src.LicenseSeats,
            Years                        = src.Years,
            MessageKey                   = null,
            LicenseAttributeLicenseValue = src.LicenseAttributeLicenseValue,
            LicenseKeycodeTypeId         = src.LicenseKeycodeTypeId,
            StorageGb                    = src.StorageGb,
            RetentionModelId             = src.RetentionModelId,
            Modules                      = src.Modules,
        };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
