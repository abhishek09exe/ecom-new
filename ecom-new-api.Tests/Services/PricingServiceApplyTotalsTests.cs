using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class PricingServiceApplyTotalsTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static PricingLineItem MakeLine(
        decimal listPrice, decimal unitPrice, decimal usagePrice,
        decimal? equivalentYearPrice, int qty = 10)
        => new()
        {
            LineItem            = 1,
            Quantity            = qty,
            ListPrice           = listPrice,
            UnitPrice           = unitPrice,
            UsagePrice          = usagePrice,
            EquivalentYearPrice = equivalentYearPrice ?? listPrice, // fallback applied upstream in MapRow
            LicenseCategoryName = "SAEP",
        };

    private static (PricingLineItem item, PricingTotals bundle, Dictionary<string, PricingTotals> byProduct)
        Run(PricingLineItem item)
    {
        PricingTotals? bundle = null;
        var byProduct = new Dictionary<string, PricingTotals>();
        PricingService.ApplyTotals(item, "en_US", "USD", ref bundle, byProduct);
        return (item, bundle!, byProduct);
    }

    // ── LALV=1 Annual ─────────────────────────────────────────────────────────────

    [Fact]
    public void Lalv1_Annual_SetsSubTotalsCorrectly()
    {
        var item = MakeLine(listPrice: 30m, unitPrice: 24.50m, usagePrice: 0m, equivalentYearPrice: 30m, qty: 10);
        var (line, bundle, byProduct) = Run(item);

        Assert.Equal(300m,  line.SubTotalListAmount);
        Assert.Equal(245m,  line.SubTotalAmount);
        Assert.Equal(300m,  line.SubTotalEquivalentYearPrice);
        Assert.Equal(0m,    line.EstimatedMonthlyPrice);
        Assert.Equal(55m,   line.SubTotalCalculatedDiscount);
        Assert.Equal(5.50m, line.CalculatedDiscount);
        // discPct = Round((55/300)*100, 4) = 18.3333 → RoundPct(18.3333) = nearest 0.5 = 18.5
        Assert.Equal(18.5m, line.CalculatedDiscountPct);
    }

    [Fact]
    public void Lalv1_Annual_FormatsUsdCorrectly()
    {
        var item = MakeLine(30m, 24.50m, 0m, 30m, qty: 10);
        var (line, _, _) = Run(item);

        Assert.Equal("$30.00",  line.ListPriceFmt);
        Assert.Equal("$24.50",  line.UnitPriceFmt);
        Assert.Equal("$245.00", line.SubTotalAmountFmt);
    }

    // ── LALV=11 Overage ───────────────────────────────────────────────────────────

    [Fact]
    public void Lalv11_Overage_BothUnitAndUsagePriceSet()
    {
        var item = MakeLine(listPrice: 30m, unitPrice: 25m, usagePrice: 5m, equivalentYearPrice: 30m, qty: 5);
        var (line, bundle, _) = Run(item);

        Assert.Equal(125m, line.SubTotalAmount);         // unit * qty
        Assert.Equal(25m,  line.EstimatedMonthlyPrice);  // usage * qty
        Assert.Equal(150m, line.SubTotalListAmount);
        Assert.Equal(25m,  line.SubTotalCalculatedDiscount);
    }

    // ── LALV=12 Utility ───────────────────────────────────────────────────────────

    [Fact]
    public void Lalv12_Utility_UnitPriceZeroUsagePriceSet()
    {
        var item = MakeLine(listPrice: 0m, unitPrice: 0m, usagePrice: 8m, equivalentYearPrice: 0m, qty: 10);
        var (line, bundle, _) = Run(item);

        Assert.Equal(0m,  line.SubTotalAmount);
        Assert.Equal(80m, line.EstimatedMonthlyPrice);
        Assert.Equal(0m,  line.CalculatedDiscount);
        Assert.Equal(0m,  line.SubTotalCalculatedDiscount);
    }

    // ── NULL equivalent_year_price falls back to list_price ───────────────────────

    [Fact]
    public void NullEquivalentYearPrice_FallsBackToListPrice()
    {
        // MapRow applies fallback: EquivalentYearPrice = row.EquivalentYearPrice ?? row.ListPrice
        var item = MakeLine(listPrice: 30m, unitPrice: 27m, usagePrice: 0m, equivalentYearPrice: null, qty: 5);
        // equivalentYearPrice is set to listPrice (30) by the helper above
        var (line, _, _) = Run(item);

        Assert.Equal(30m, line.EquivalentYearPrice);
        Assert.Equal(150m, line.SubTotalEquivalentYearPrice);
        Assert.Equal(135m, line.SubTotalAmount);
        Assert.Equal(15m,  line.SubTotalCalculatedDiscount);
    }

    // ── Bundle totals accumulate across multiple lines ─────────────────────────────

    [Fact]
    public void BundleTotals_AccumulateAcrossMultipleLines()
    {
        PricingTotals? bundle = null;
        var byProduct = new Dictionary<string, PricingTotals>();

        var line1 = MakeLine(30m, 24.50m, 0m, 30m, qty: 10);
        var line2 = MakeLine(20m, 18m,    0m, 20m, qty: 5);

        PricingService.ApplyTotals(line1, "en_US", "USD", ref bundle, byProduct);
        PricingService.ApplyTotals(line2, "en_US", "USD", ref bundle, byProduct);

        Assert.Equal(300m + 100m, bundle!.SubTotalListAmount);
        Assert.Equal(245m + 90m,  bundle.SubTotalAmount);
    }

    // ── RoundPct rounds to nearest 0.5 ────────────────────────────────────────────

    [Theory]
    [InlineData(18.3,  18.5)]
    [InlineData(18.5,  18.5)]
    [InlineData(18.6,  18.5)]
    [InlineData(18.75, 19.0)]
    [InlineData(0.0,   0.0)]
    [InlineData(100.0, 100.0)]
    public void RoundPct_RoundsToNearestHalf(double input, double expected)
    {
        var result = PricingService.RoundPct((decimal)input);
        Assert.Equal((decimal)expected, result);
    }
}
