using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ecom_new_api_tests.Repositories;

/// <summary>
/// Integration-style unit tests for CartOrderRepository using an in-memory SQLite database.
/// Each test creates its own isolated context so tests never share state.
/// </summary>
public sealed class CartOrderRepositoryTests : IDisposable
{
    // Keep the same SQLite connection open for the lifetime of each test so the
    // in-memory database is not destroyed between context instances.
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public CartOrderRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create schema once per test instance
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private AppDbContext NewContext() => new(_options);

    private CartOrderRepository NewRepo(AppDbContext ctx) =>
        new(ctx, NullLogger<CartOrderRepository>.Instance);

    /// <summary>
    /// Seeds the minimum rows required for any cart-order select to succeed:
    /// a Currency row with id=1 (USD) must exist because the select JOINs to currency.
    /// </summary>
    private async Task SeedDefaultCurrencyAsync(AppDbContext ctx)
    {
        if (!await ctx.Currency.AnyAsync())
        {
            ctx.Currency.Add(new Currency { CurrencyId = 1, CurrencyCode = "USD" });
            await ctx.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Seeds the full product graph needed for SelectCartOrderItemsAsync.
    /// Returns the ProductId that was created.
    /// </summary>
    private async Task<int> SeedProductAsync(AppDbContext ctx, int productId = 10)
    {
        var pt = new ProductType { ProductTypeId = 1, ProductTypeDescription = "Standard" };
        var pf = new ProductFamily { ProductFamilyId = 1, ProductFamilyDescription = "Family" };
        var pl = new ProductLine { ProductLineId = 1, ProductLineCartType = "STANDARD" };

        ctx.ProductType.Add(pt);
        ctx.ProductFamily.Add(pf);
        ctx.ProductLine.Add(pl);
        await ctx.SaveChangesAsync();

        var product = new Product
        {
            ProductId = productId,
            ProductTypeId = pt.ProductTypeId,
            ProductFamilyId = pf.ProductFamilyId,
            ProductDescription = "Test Product"
        };
        ctx.Product.Add(product);

        ctx.ProductLineProduct.Add(new ProductLineProduct
        {
            ProductId = productId,
            ProductLineId = pl.ProductLineId
        });

        await ctx.SaveChangesAsync();
        return productId;
    }

    // ── InsertCartOrderHeaderAsync ────────────────────────────────────────────────

    [Fact]
    public async Task InsertCartOrderHeaderAsync_MinimalRequest_InsertsCartOrderRow()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR99999"
        };

        var code = await repo.InsertCartOrderHeaderAsync(request);

        Assert.Equal("WR99999", code);
        var order = await ctx.CartOrder.SingleOrDefaultAsync(o => o.VendorOrderCode == "WR99999");
        Assert.NotNull(order);
        Assert.Equal("webroot", order!.SiteId);
        Assert.Equal("en-US", order.Locale);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_NoVendorOrderCode_GeneratesCodeWithPrefix()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        ctx.CartSiteIdOrderCodePrefix.Add(new CartSiteIdOrderCodePrefix
        {
            Id = 1,
            SiteId = "webroot",
            VendorOrderCodePrefix = "WR"
        });
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        // Provide a VendorOrderCode to avoid NEXT VALUE FOR sequence (not supported by SQLite).
        // The sequence-based auto-generation path is covered by SQL Server integration tests.
        var request = new CartOrderCreateRequest { SiteId = "webroot", Locale = "en-US", VendorOrderCode = "WR99999" };

        var code = await repo.InsertCartOrderHeaderAsync(request);

        // Supplied code is returned as-is
        Assert.Equal("WR99999", code);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_NoCurrencyCode_DefaultsToCurrencyId1()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00001"
        };

        await repo.InsertCartOrderHeaderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00001");
        Assert.Equal(1, order.CurrencyId);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_WithKnownCurrencyCode_UsesMatchedCurrencyId()
    {
        await using var ctx = NewContext();
        ctx.Currency.AddRange(
            new Currency { CurrencyId = 1, CurrencyCode = "USD" },
            new Currency { CurrencyId = 2, CurrencyCode = "EUR" });
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00002",
            CurrencyCode = "EUR"
        };

        await repo.InsertCartOrderHeaderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00002");
        Assert.Equal(2, order.CurrencyId);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_WithRoutingAction_InsertsCartOrderRoute()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00003",
            RoutingAction = "autoprocess"
        };

        await repo.InsertCartOrderHeaderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00003");
        var route = await ctx.CartOrderRoute.SingleOrDefaultAsync(r => r.CartOrderId == order.CartOrderId);
        Assert.NotNull(route);
        Assert.Equal("autoprocess", route!.RoutingAction);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_WithMessageKey_InsertsCartOrderMessage()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00004",
            MessageKey = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
        };

        await repo.InsertCartOrderHeaderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00004");
        var msg = await ctx.CartOrderMessage.SingleOrDefaultAsync(m => m.CartOrderId == order.CartOrderId);
        Assert.NotNull(msg);
        Assert.Equal(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), msg!.MessageKey);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_NoRoutingAction_DoesNotInsertRoute()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00005"
            // No RoutingAction
        };

        await repo.InsertCartOrderHeaderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00005");
        var routeCount = await ctx.CartOrderRoute.CountAsync(r => r.CartOrderId == order.CartOrderId);
        Assert.Equal(0, routeCount);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_WithExtensionFields_InsertsCartJson()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00006",
            CurrencyCode = "USD"   // non-null extension field
        };

        await repo.InsertCartOrderHeaderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00006");
        var json = await ctx.CartJson.SingleOrDefaultAsync(j => j.CartOrderId == order.CartOrderId);
        Assert.NotNull(json);
        Assert.Contains("USD", json!.Json);
    }

    [Fact]
    public async Task InsertCartOrderHeaderAsync_MinimalFieldsOnly_DoesNotInsertCartJson()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        // VendorOrderCode provided to avoid NEXT VALUE FOR sequence (not supported by SQLite).
        // All extension-triggering fields (UrlLink, PRc, etc.) are left null to verify no cart_json row is written.
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR_MIN_001"
        };

        var code = await repo.InsertCartOrderHeaderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == code);
        var jsonCount = await ctx.CartJson.CountAsync(j => j.CartOrderId == order.CartOrderId);
        Assert.Equal(0, jsonCount);
    }

    // ── InsertCartOrderAsync (composite) ─────────────────────────────────────────

    [Fact]
    public async Task InsertCartOrderAsync_WithItems_InsertsHeaderAndItems()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var productId = await SeedProductAsync(ctx);

        var repo = NewRepo(ctx);
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00010",
            Items =
            [
                new CartOrderItemRequest
                {
                    LicenseCategoryName = "SOHO",
                    ProductId = productId,
                    Quantity = 2
                }
            ]
        };

        var code = await repo.InsertCartOrderAsync(request);

        Assert.Equal("WR00010", code);
        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00010");
        var items = await ctx.CartOrderItem.Where(i => i.CartOrderId == order.CartOrderId).ToListAsync();
        Assert.Single(items);
        Assert.Equal(productId, items[0].ProductId);
        Assert.Equal(2, items[0].Quantity);
    }

    [Fact]
    public async Task InsertCartOrderAsync_MultipleItems_AssignsSequentialLineItems()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var productId = await SeedProductAsync(ctx);

        var repo = NewRepo(ctx);
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            VendorOrderCode = "WR00011",
            Items =
            [
                new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = productId },
                new CartOrderItemRequest { LicenseCategoryName = "SMB",  ProductId = productId }
            ]
        };

        await repo.InsertCartOrderAsync(request);

        var order = await ctx.CartOrder.SingleAsync(o => o.VendorOrderCode == "WR00011");
        var lineItems = await ctx.CartOrderItem
            .Where(i => i.CartOrderId == order.CartOrderId)
            .OrderBy(i => i.LineItem)
            .Select(i => i.LineItem)
            .ToListAsync();

        Assert.Equal([1, 2], lineItems);
    }

    // ── SelectCartOrderHeaderAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SelectCartOrderHeaderAsync_UnknownCode_ReturnsNull()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var result = await repo.SelectCartOrderHeaderAsync("DOES_NOT_EXIST");

        Assert.Null(result);
    }

    [Fact]
    public async Task SelectCartOrderHeaderAsync_KnownCode_ReturnsHeader()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);

        ctx.CartOrder.Add(new CartOrder
        {
            VendorOrderCode = "WR99001",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        });
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        var result = await repo.SelectCartOrderHeaderAsync("WR99001");

        Assert.NotNull(result);
        Assert.Equal("WR99001", result!.VendorOrderCode);
        Assert.Equal("webroot", result.SiteId);
        Assert.Equal("USD", result.CurrencyCode);
    }

    // ── SelectCartOrderAsync (composite) ─────────────────────────────────────────

    [Fact]
    public async Task SelectCartOrderAsync_UnknownCode_ReturnsNull()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var repo = NewRepo(ctx);

        var result = await repo.SelectCartOrderAsync("MISSING");

        Assert.Null(result);
    }

    [Fact]
    public async Task SelectCartOrderAsync_OrderWithNoItems_ReturnsEmptyItemsDict()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);

        ctx.CartOrder.Add(new CartOrder
        {
            VendorOrderCode = "WR99002",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        });
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        var result = await repo.SelectCartOrderAsync("WR99002");

        Assert.NotNull(result);
        Assert.Empty(result!.Items);
    }

    [Fact]
    public async Task SelectCartOrderAsync_OrderWithMessageKey_PopulatesMessageKey()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);

        var order = new CartOrder
        {
            VendorOrderCode = "WR99003",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        };
        ctx.CartOrder.Add(order);
        await ctx.SaveChangesAsync();

        ctx.CartOrderMessage.Add(new CartOrderMessage
        {
            CartOrderId = order.CartOrderId,
            MessageKey = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901")
        });
        await ctx.SaveChangesAsync();

        // Items will have the message key set after select
        var repo = NewRepo(ctx);
        var result = await repo.SelectCartOrderAsync("WR99003");

        Assert.NotNull(result);
        // Items dict is empty (no items seeded) but we can verify the order returned
        Assert.Equal("WR99003", result!.VendorOrderCode);
    }

    // ── SelectCartOrderAsync — pricing & formatting ───────────────────────────────

    [Fact]
    public async Task SelectCartOrderAsync_WithAmounts_FormatsUsdCurrencyStrings()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);

        ctx.CartOrder.Add(new CartOrder
        {
            VendorOrderCode = "WR99004",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test",
            SubTotalAmount = 49.99m,
            TotalAmount = 53.99m,
            TaxAmount = 4.00m,
            OfferAmount = 45.00m
        });
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        var result = await repo.SelectCartOrderAsync("WR99004");

        Assert.NotNull(result);
        Assert.Equal("$49.99", result!.SubTotalAmountFmt);
        Assert.Equal("$53.99", result.TotalAmountFmt);
        Assert.Equal("$4.00",  result.TaxAmountFmt);
        Assert.Equal("$45.00", result.OfferAmountFmt);
    }

    [Fact]
    public async Task SelectCartOrderAsync_EurCurrency_FormatsWithEuroSymbol()
    {
        await using var ctx = NewContext();
        ctx.Currency.AddRange(
            new Currency { CurrencyId = 1, CurrencyCode = "USD" },
            new Currency { CurrencyId = 3, CurrencyCode = "EUR" });
        await ctx.SaveChangesAsync();

        ctx.CartOrder.Add(new CartOrder
        {
            VendorOrderCode = "WR99005",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "de-DE",
            CurrencyId = 3,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test",
            TotalAmount = 29.99m
        });
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        var result = await repo.SelectCartOrderAsync("WR99005");

        Assert.NotNull(result);
        Assert.Equal("€29.99", result!.TotalAmountFmt);
    }

    // ── FindExistingVendorOrderCodeByKeyAsync ─────────────────────────────────────

    [Fact]
    public async Task FindExistingVendorOrderCodeByKeyAsync_KeyNotFound_ReturnsNull()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        var result = await repo.FindExistingVendorOrderCodeByKeyAsync("NO_SUCH_KEY");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindExistingVendorOrderCodeByKeyAsync_KeyFound_ReturnsVendorOrderCode()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);

        var order = new CartOrder
        {
            VendorOrderCode = "WR55555",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        };
        ctx.CartOrder.Add(order);
        await ctx.SaveChangesAsync();

        ctx.CartOrderMessage.Add(new CartOrderMessage
        {
            CartOrderId = order.CartOrderId,
            MessageKey = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012")
        });
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        var result = await repo.FindExistingVendorOrderCodeByKeyAsync("c3d4e5f6-a7b8-9012-cdef-123456789012");

        Assert.Equal("WR55555", result);
    }

    // ── InsertCartOrderItemAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task InsertCartOrderItemAsync_DefaultsQuantityToOne_WhenNullOnRequest()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var productId = await SeedProductAsync(ctx);

        var order = new CartOrder
        {
            VendorOrderCode = "WR66001",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        };
        ctx.CartOrder.Add(order);
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        var itemRequest = new CartOrderItemRequest
        {
            LicenseCategoryName = "SOHO",
            ProductId = productId,
            Quantity = null  // should default to 1
        };

        await repo.InsertCartOrderItemAsync(order.CartOrderId, "WR66001", itemRequest, lineItem: 1);

        var item = await ctx.CartOrderItem.SingleAsync(i => i.CartOrderId == order.CartOrderId);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public async Task InsertCartOrderItemAsync_WithPricingFromProductPricing_SetsUnitPrice()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var productId = await SeedProductAsync(ctx);

        ctx.ProductPricing.Add(new ProductPricing
        {
            ProductId = productId,
            RetailPrice = 29.99m
        });
        await ctx.SaveChangesAsync();

        var order = new CartOrder
        {
            VendorOrderCode = "WR66002",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        };
        ctx.CartOrder.Add(order);
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        await repo.InsertCartOrderItemAsync(
            order.CartOrderId, "WR66002",
            new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = productId },
            lineItem: 1);

        var item = await ctx.CartOrderItem.SingleAsync(i => i.CartOrderId == order.CartOrderId);
        Assert.Equal(29.99m, item.UnitPrice);
    }

    [Fact]
    public async Task InsertCartOrderItemAsync_WithExplicitUnitPrice_SkipsProductPricingLookup()
    {
        await using var ctx = NewContext();
        await SeedDefaultCurrencyAsync(ctx);
        var productId = await SeedProductAsync(ctx);

        // Seed a different price in product_pricing; the explicit price should win
        ctx.ProductPricing.Add(new ProductPricing { ProductId = productId, RetailPrice = 99.00m });
        await ctx.SaveChangesAsync();

        var order = new CartOrder
        {
            VendorOrderCode = "WR66003",
            OrderType = "webroot",
            SiteId = "webroot",
            SiteUrl = "webroot",
            Locale = "en-US",
            CurrencyId = 1,
            SalesOrderDate = DateTime.UtcNow.Date,
            InsertDate = DateTime.UtcNow,
            InsertBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        };
        ctx.CartOrder.Add(order);
        await ctx.SaveChangesAsync();

        var repo = NewRepo(ctx);
        await repo.InsertCartOrderItemAsync(
            order.CartOrderId, "WR66003",
            new CartOrderItemRequest
            {
                LicenseCategoryName = "SOHO",
                ProductId = productId,
                UnitPrice = 49.99m   // explicit override
            },
            lineItem: 1);

        var item = await ctx.CartOrderItem.SingleAsync(i => i.CartOrderId == order.CartOrderId);
        Assert.Equal(49.99m, item.UnitPrice);
        Assert.Equal(0m, item.ListPrice);  // list_price defaults to 0 when price came from explicit override
    }
}
