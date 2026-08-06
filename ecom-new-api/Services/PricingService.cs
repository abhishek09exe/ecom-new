using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories;

namespace ecom_new_api.Services;

public class PricingService : IPricingService
{
    private readonly PricingRepository _repo;
    private readonly MessageKeyService _msgKey;
    private readonly CurrencyService   _currency;

    public PricingService(PricingRepository repo, MessageKeyService msgKey, CurrencyService currency)
        => (_repo, _msgKey, _currency) = (repo, msgKey, currency);

    public async Task<BundlePricingResponse> GetBundlePricingAsync(BundlePricingRequest request)
    {
        var locale = request.Locale ?? "en_US";
        var (currencyCode, currencySymbol) = _currency.GetCurrency(locale);

        var response = new BundlePricingResponse
        {
            CurrencyCode   = currencyCode,
            CurrencySymbol = currencySymbol
        };

        var allItems     = new List<PricingLineItem>();
        PricingTotals?   bundleTotals = null;
        var productTotals = new Dictionary<string, PricingTotals>();

        foreach (var bundle in request.Items)
        {
            var resolved = await _msgKey.ResolveAsync(bundle, locale);
            var (itemJson, bundleJson) = BuildSpInput(resolved, locale, request.LicenseKeycodeTypeId);
            var rows = await _repo.GetConfiguratorPricingAsync(itemJson, bundleJson);

            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.LicenseCategoryName)) continue;
                var line = MapRow(row, resolved);
                ApplyTotals(line, locale, currencyCode, ref bundleTotals, productTotals);
                allItems.Add(line);
            }
        }

        response.Items         = allItems;
        response.Totals        = bundleTotals ?? new PricingTotals();
        response.ProductTotals = productTotals;
        return response;
    }

    // ── SP input builders ────────────────────────────────────────────────────────

    private (string itemJson, string bundleJson) BuildSpInput(
        ResolvedBundleContext r, string locale, int defaultTypeId)
    {
        var item   = r.Bundle;
        var typeId = item.LicenseKeycodeTypeId > 0 ? item.LicenseKeycodeTypeId : defaultTypeId;

        var spItems = new List<object> { MakeSpItem(item, locale, typeId, 1, 1) };
        foreach (var m in item.Modules)
            spItems.Add(new
            {
                license_category_name           = m.LicenseCategoryName,
                license_seats                   = m.LicenseSeats,
                storage_gb                      = m.StorageGb,
                retention_model_id              = (int?)null,
                years                           = m.Years,
                license_keycode_type_id         = typeId,
                locale,
                license_attribute_license_value = item.LicenseAttributeLicenseValue,
                start_date                      = "",
                expiration_date                 = "",
                cart_item_bundle_id             = 1,
                item_hierarchy_id               = 2,
                vendor_order_item_code          = (string?)null,
                discount                        = (decimal?)null,
                cart_discount_method_id         = (int?)null
            });

        var bundleObj = new Dictionary<string, object?>
        {
            ["locale"]                          = locale,
            ["keycode"]                         = r.Keycode,
            ["license_attribute_license_value"] = item.LicenseAttributeLicenseValue,
            ["license_keycode_type_id"]         = typeId,
            ["cart_discount_id"]                = r.CartDiscountId,
            ["message_campaign_name"]           = r.MessageCampaignName,
        };
        if (r.IncludeMessageKeyInBundle) bundleObj["message_key"] = item.MessageKey;

        return (
            JsonSerializer.Serialize(spItems,   SnakeCaseOpts),
            JsonSerializer.Serialize(bundleObj, SnakeCaseOpts)
        );
    }

    private static object MakeSpItem(
        BundlePricingItem i, string locale, int typeId, int bundleId, int hierarchyId)
        => new
        {
            license_category_name           = i.LicenseCategoryName,
            license_seats                   = i.LicenseSeats,
            storage_gb                      = i.StorageGb,
            retention_model_id              = i.RetentionModelId,
            years                           = i.Years,
            license_keycode_type_id         = typeId,
            locale,
            license_attribute_license_value = i.LicenseAttributeLicenseValue,
            start_date                      = "",
            expiration_date                 = "",
            cart_item_bundle_id             = bundleId,
            item_hierarchy_id               = hierarchyId,
            vendor_order_item_code          = (string?)null,
            discount                        = (decimal?)null,
            cart_discount_method_id         = (int?)null
        };

    // ── Row mapper ───────────────────────────────────────────────────────────────

    private static PricingLineItem MapRow(ConfiguratorPricingResult row, ResolvedBundleContext r)
        => new()
        {
            LineItem                   = row.LineItem,
            Quantity                   = row.Quantity,
            ListPrice                  = row.ListPrice,
            UnitPrice                  = row.UnitPrice,
            UsagePrice                 = row.UsagePrice,
            EquivalentYearPrice        = row.EquivalentYearPrice ?? row.ListPrice,
            OrderItemOfferAmount       = row.OrderItemOfferAmount?.ToString(),
            ProductDescription         = row.ProductDescription,
            ProductTypeDescription     = row.ProductTypeDescription,
            LicenseCategoryName        = row.LicenseCategoryName,
            LicenseCategoryDescription = row.LicenseCategoryDescription ?? string.Empty,
            ProductFamilyDescription   = row.ProductFamilyDescription,
            StartDate                  = row.StartDate?.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            ExpirationDate             = row.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            CartItemBundleId           = row.CartItemBundleId,
            ItemHierarchyId            = row.ItemHierarchyId,
            LicenseKeycodeTypeId       = row.LicenseKeycodeTypeId,
            DependentCartOrderItemId   = row.DependentCartOrderItemId,
            StorageGb                  = row.StorageGb,
            UsagePricingModelId        = row.UsagePricingModelId,
            RetentionModelId           = row.RetentionModelId,
            RetentionTerm              = row.RetentionTerm,
            RetentionModelName         = row.RetentionModelName,
            ActualStorageQuantity      = row.ActualStorageQuantity?.ToString(),
            MessageKey                 = r.Bundle.MessageKey,
            LicenseAttributeLicenseValue = r.Bundle.LicenseAttributeLicenseValue,
            CartDiscountId             = r.CartDiscountId,
        };

    // ── Totals & formatting ──────────────────────────────────────────────────────

    public static void ApplyTotals(
        PricingLineItem item, string locale, string currencyCode,
        ref PricingTotals? bundle, Dictionary<string, PricingTotals> byProduct)
    {
        var qty       = item.Quantity;
        var eqYear    = item.EquivalentYearPrice;
        var subEqYear = Math.Round(eqYear * qty, 2);
        var subList   = Math.Round(item.ListPrice  * qty, 2);
        var subUnit   = Math.Round(item.UnitPrice   * qty, 2);
        var subUsage  = Math.Round(item.UsagePrice  * qty, 2);
        decimal calcDisc = 0, subCalcDisc = 0, discPct = 0;

        if (eqYear > item.UnitPrice)
        {
            calcDisc    = Math.Round(eqYear - item.UnitPrice, 2);
            subCalcDisc = Math.Round(subEqYear - subUnit, 2);
            if (subEqYear != 0) discPct = Math.Round((subCalcDisc / subEqYear) * 100, 4);
        }

        item.CalculatedDiscount          = calcDisc;
        item.CalculatedDiscountPct       = RoundPct(discPct);
        item.SubTotalCalculatedDiscount  = subCalcDisc;
        item.SubTotalListAmount          = subList;
        item.SubTotalAmount              = subUnit;
        item.SubTotalEquivalentYearPrice = subEqYear;
        item.EstimatedMonthlyPrice       = subUsage;
        FormatLineItem(item, locale, currencyCode);

        bundle ??= new PricingTotals();
        Accumulate(bundle, subEqYear, subList, subUnit, subUsage, subCalcDisc, locale, currencyCode);

        var cat = item.LicenseCategoryName;
        if (!byProduct.ContainsKey(cat)) byProduct[cat] = new PricingTotals();
        Accumulate(byProduct[cat], subEqYear, subList, subUnit, subUsage, subCalcDisc, locale, currencyCode);
    }

    private static void Accumulate(
        PricingTotals t, decimal eqYear, decimal list, decimal unit, decimal usage, decimal disc,
        string locale, string cc)
    {
        t.SubTotalEquivalentYearPrice += eqYear;
        t.SubTotalListAmount          += list;
        t.SubTotalAmount              += unit;
        t.EstimatedMonthlyPrice       += usage;
        t.SubTotalCalculatedDiscount  += disc;
        if (t.SubTotalListAmount != 0)
            t.CalculatedDiscountPct = RoundPct((t.SubTotalCalculatedDiscount / t.SubTotalListAmount) * 100);
        t.SubTotalEquivalentYearPriceFmt = Fmt(t.SubTotalEquivalentYearPrice, locale, cc);
        t.SubTotalListAmountFmt          = Fmt(t.SubTotalListAmount, locale, cc);
        t.SubTotalAmountFmt              = Fmt(t.SubTotalAmount, locale, cc);
        t.EstimatedMonthlyPriceFmt       = Fmt(t.EstimatedMonthlyPrice, locale, cc);
        t.SubTotalCalculatedDiscountFmt  = Fmt(t.SubTotalCalculatedDiscount, locale, cc);
    }

    private static void FormatLineItem(PricingLineItem i, string locale, string cc)
    {
        i.ListPriceFmt                   = Fmt(i.ListPrice, locale, cc);
        i.UnitPriceFmt                   = Fmt(i.UnitPrice, locale, cc);
        i.UsagePriceFmt                  = Fmt(i.UsagePrice, locale, cc);
        i.EquivalentYearPriceFmt         = Fmt(i.EquivalentYearPrice, locale, cc);
        i.CalculatedDiscountFmt          = Fmt(i.CalculatedDiscount, locale, cc);
        i.SubTotalCalculatedDiscountFmt  = Fmt(i.SubTotalCalculatedDiscount, locale, cc);
        i.SubTotalListAmountFmt          = Fmt(i.SubTotalListAmount, locale, cc);
        i.SubTotalAmountFmt              = Fmt(i.SubTotalAmount, locale, cc);
        i.SubTotalEquivalentYearPriceFmt = Fmt(i.SubTotalEquivalentYearPrice, locale, cc);
        i.EstimatedMonthlyPriceFmt       = Fmt(i.EstimatedMonthlyPrice, locale, cc);
    }

    /// <summary>Rounds to nearest 0.5 — matches legacy pricing behaviour.</summary>
    public static decimal RoundPct(decimal pct)
        => Math.Round(pct * 2, 0, MidpointRounding.AwayFromZero) / 2;

    private static string Fmt(decimal v, string locale, string _)
        => v.ToString("C", CultureInfo.CreateSpecificCulture(locale.Replace("_", "-")));

    private static readonly JsonSerializerOptions SnakeCaseOpts = new()
    {
        PropertyNamingPolicy         = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition       = JsonIgnoreCondition.WhenWritingNull
    };
}
