using System.Globalization;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Pricing;

namespace ecom_new_api.Services.Pricing;

public class PricingService : IPricingService
{
    private readonly IPricingRepository _repo;
    private readonly MessageKeyService _msgKey;
    private readonly CurrencyService   _currency;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        IPricingRepository repo,
        MessageKeyService msgKey,
        CurrencyService currency,
        ILogger<PricingService> logger)
        => (_repo, _msgKey, _currency, _logger) = (repo, msgKey, currency, logger);

    public async Task<BundlePricingResponse> GetBundlePricingAsync(BundlePricingRequest request)
    {
        var locale = request.Locale ?? "en_US";
        var (currencyCode, currencySymbol) = _currency.GetCurrency(locale);

        _logger.LogDebug(
            "Pricing {BundleCount} bundle(s) for locale={Locale} resolved to currency={CurrencyCode}",
            request.Items.Count, locale, currencyCode);

        var response = new BundlePricingResponse
        {
            CurrencyCode   = currencyCode,
            CurrencySymbol = currencySymbol
        };

        var allItems      = new List<PricingLineItem>();
        PricingTotals?    bundleTotals  = null;
        var productTotals = new Dictionary<string, PricingTotals>();

        // Build the full list of pricing calls (primary item + every module, across all bundle
        // items in the request) up front, invoking the repository synchronously in the same
        // order as before, then await them all together. This turns what used to be N sequential
        // round-trip chains into genuinely concurrent DB work while preserving call order for
        // any callers/mocks that depend on invocation sequence.
        var pending = new List<(ResolvedBundleContext Context, int Lalv, Task<List<ConfiguratorPricingResult>> RowsTask)>();

        foreach (var bundle in request.Items)
        {
            var resolved = await _msgKey.ResolveAsync(bundle, locale);
            var lalv     = bundle.LicenseAttributeLicenseValue ?? 1;

            // ── Primary item ─────────────────────────────────────────────────────
            var typeId = bundle.LicenseKeycodeTypeId > 0
                ? bundle.LicenseKeycodeTypeId
                : request.LicenseKeycodeTypeId;

            var primaryInput = new BundleItemPricingInput(
                LicenseCategoryName  : bundle.LicenseCategoryName,
                LicenseSeats         : bundle.LicenseSeats,
                Years                : bundle.Years,
                LicenseKeycodeTypeId : typeId,
                Locale               : locale,
                CartItemBundleId     : 1,
                ItemHierarchyId      : 1,
                StorageGb            : bundle.StorageGb,
                RetentionModelId     : bundle.RetentionModelId
            );

            pending.Add((resolved, lalv, _repo.GetItemPricingAsync([primaryInput])));

            // ── Modules — each priced as an independent EF Core query ────────
            foreach (var module in bundle.Modules)
            {
                var moduleItem = new BundlePricingItem
                {
                    LicenseCategoryName          = module.LicenseCategoryName,
                    LicenseSeats                 = module.LicenseSeats,
                    Years                        = module.Years,
                    MessageKey                   = null,
                    LicenseAttributeLicenseValue = lalv,
                    LicenseKeycodeTypeId         = typeId,
                    StorageGb                    = module.StorageGb,
                    Modules                      = new List<BundleModule>()
                };

                var moduleContext = new ResolvedBundleContext
                {
                    Bundle                   = moduleItem,
                    Keycode                  = resolved.Keycode,
                    CartDiscountId           = resolved.CartDiscountId,
                    MessageCampaignName      = resolved.MessageCampaignName,
                    IncludeMessageKeyInBundle = false
                };

                var moduleInput = new BundleItemPricingInput(
                    LicenseCategoryName  : module.LicenseCategoryName,
                    LicenseSeats         : module.LicenseSeats,
                    Years                : module.Years,
                    LicenseKeycodeTypeId : typeId,
                    Locale               : locale,
                    CartItemBundleId     : 1,
                    ItemHierarchyId      : 2,
                    StorageGb            : module.StorageGb
                );

                pending.Add((moduleContext, lalv, _repo.GetItemPricingAsync([moduleInput])));
            }
        }

        await Task.WhenAll(pending.Select(p => p.RowsTask)).ConfigureAwait(false);

        foreach (var (context, lalv, rowsTask) in pending)
        {
            var rows = await rowsTask;

            if (rows.Count == 0)
                _logger.LogWarning(
                    "No pricing rows returned for bundle category={Category} seats={Seats} years={Years}",
                    context.Bundle.LicenseCategoryName, context.Bundle.LicenseSeats, context.Bundle.Years);

            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.LicenseCategoryName)) continue;
                var line = MapRow(row, context, lalv);
                ApplyTotals(line, locale, currencyCode, ref bundleTotals, productTotals);
                allItems.Add(line);
            }
        }

        response.Items         = allItems;
        response.Totals        = bundleTotals ?? new PricingTotals();
        response.ProductTotals = productTotals;
        return response;
    }

    // ── Row mapper ───────────────────────────────────────────────────────────────

    private static PricingLineItem MapRow(ConfiguratorPricingResult row, ResolvedBundleContext r, int lalv)
        => new()
        {
            LineItem                     = row.LineItem,
            Quantity                     = row.Quantity,
            ListPrice                    = row.ListPrice,
            UnitPrice                    = row.UnitPrice,
            UsagePrice                   = row.UsagePrice > 0
                                             ? row.UsagePrice
                                             : lalv == 12 && row.UnitPrice > 0
                                                 ? Math.Round(row.UnitPrice / 12, 2)
                                                 : row.UsagePrice,
            EquivalentYearPrice          = row.EquivalentYearPrice ?? row.ListPrice,
            OrderItemOfferAmount         = row.OrderItemOfferAmount?.ToString(),
            ProductDescription           = row.ProductDescription,
            ProductTypeDescription       = row.ProductTypeDescription,
            LicenseCategoryName          = row.LicenseCategoryName,
            LicenseCategoryDescription   = row.LicenseCategoryDescription ?? string.Empty,
            ProductFamilyDescription     = row.ProductFamilyDescription,
            StartDate                    = row.StartDate?.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            ExpirationDate               = row.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            CartItemBundleId             = row.CartItemBundleId,
            ItemHierarchyId              = row.ItemHierarchyId,
            LicenseKeycodeTypeId         = row.LicenseKeycodeTypeId,
            DependentCartOrderItemId     = row.DependentCartOrderItemId,
            StorageGb                    = row.StorageGb,
            UsagePricingModelId          = row.UsagePricingModelId,
            RetentionModelId             = row.RetentionModelId,
            RetentionTerm                = row.RetentionTerm,
            RetentionModelName           = row.RetentionModelName,
            ActualStorageQuantity        = row.ActualStorageQuantity?.ToString(),
            MessageKey                   = r.Bundle.MessageKey,
            LicenseAttributeLicenseValue = lalv,
            CartDiscountId               = r.CartDiscountId,
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
}
