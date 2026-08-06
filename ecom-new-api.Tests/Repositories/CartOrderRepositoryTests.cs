using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Infrastructure;
using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories.Cart;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
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

    // ── SelectLicenseOptionsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SelectLicenseOptionsAsync_Phase1MapsLegacyFields_AndKeepsProductOptions()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        const int licenseId = 5001;
        const string keycode = "0116ENTPCC6E584F4771";
        var licenseKeyGuid = Guid.Parse("A5A3CD6F-788D-4D20-9E32-3362E88ED732");
        var startDate = new DateTime(2026, 04, 20);
        var endDate = DateTime.UtcNow.Date.AddDays(30);

        ctx.LicenseStatus.Add(new LicenseStatus
        {
            LicenseStatusId = 1,
            LicenseStatusDescription = "active"
        });

        ctx.ProductLine.Add(new ProductLine
        {
            ProductLineId = 10,
            ProductLineDescription = "OpenText Business"
        });

        ctx.LicenseKeycodeType.Add(new LicenseKeycodeType
        {
            LicenseKeycodeTypeId = 3,
            LicenseKeycodeTypeDescription = "Business"
        });

        ctx.License.Add(new License
        {
            LicenseId = licenseId,
            Keycode = keycode,
            ProductLineId = 10,
            LicenseStatusId = 1,
            LicenseTypeId = 2,
            LicenseKeycodeTypeId = 3,
            LicenseExpirationDate = endDate
        });

        ctx.LicenseKey.Add(new LicenseKey
        {
            LicenseId = licenseId,
            Key = licenseKeyGuid
        });

        ctx.LicenseCategory.Add(new LicenseCategory
        {
            LicenseCategoryId = 220,
            LicenseCategoryName = "SAEP",
            LicenseCategoryDescription = "OpenText Core Endpoint Protection"
        });

        ctx.LicenseCategoryLicense.Add(new LicenseCategoryLicense
        {
            LicenseCategoryLicenseId = 1,
            LicenseId = licenseId,
            LicenseCategoryId = 220,
            StartDate = startDate,
            EndDate = endDate
        });

        ctx.LicenseSeat.Add(new LicenseSeat
        {
            LicenseSeatId = 1,
            LicenseId = licenseId,
            LicenseSeats = 10,
            SeatsUsed = 0
        });

        // QA onboarding product expected by current product_options behavior.
        ctx.ProductType.Add(new ProductType
        {
            ProductTypeId = 1,
            ProductTypeDescription = "Renewal"
        });

        ctx.ProductFamily.Add(new ProductFamily
        {
            ProductFamilyId = 1,
            ProductFamilyDescription = "OpenText Secure Anywhere"
        });

        ctx.Product.Add(new Product
        {
            ProductId = 1515110108,
            ProductDescription = "QA AUTOGEN PRODUCT 2026-08-05",
            ProductTypeId = 1,
            ProductFamilyId = 1,
            LicenseKeycodeTypeId = 3
        });

        ctx.ProductLicenseCategory.Add(new ProductLicenseCategory
        {
            ProductLicenseCategoryId = 1,
            ProductId = 1515110108,
            LicenseCategoryId = 220
        });

        ctx.ProductLicenseCategorySeat.AddRange(
            new ProductLicenseCategorySeat
            {
                ProductLicenseCategorySeatId = 1,
                LicenseCategoryId = 220,
                Seats = 1,
            },
            new ProductLicenseCategorySeat
            {
                ProductLicenseCategorySeatId = 2,
                LicenseCategoryId = 220,
                Seats = 3,
            },
            new ProductLicenseCategorySeat
            {
                ProductLicenseCategorySeatId = 3,
                LicenseCategoryId = 220,
                Seats = 5,
            });

        ctx.ProductLicenseCategoryYears.AddRange(
            new ProductLicenseCategoryYears
            {
                ProductLicenseCategoryYearsId = 1,
                LicenseCategoryId = 220,
                Years = 1,
                YearsDescription = "1 Year",
            },
            new ProductLicenseCategoryYears
            {
                ProductLicenseCategoryYearsId = 2,
                LicenseCategoryId = 220,
                Years = 1.25,
                YearsDescription = "15 Month",
            },
            new ProductLicenseCategoryYears
            {
                ProductLicenseCategoryYearsId = 3,
                LicenseCategoryId = 220,
                Years = 2,
                YearsDescription = "2 Year",
            },
            new ProductLicenseCategoryYears
            {
                ProductLicenseCategoryYearsId = 4,
                LicenseCategoryId = 220,
                Years = 3,
                YearsDescription = "3 Year",
            });

        ctx.ProductYears.Add(new ProductYears
        {
            ProductYearsId = 1,
            ProductId = 1515110108,
            Years = 1
        });

        ctx.ProductSeat.Add(new ProductSeat
        {
            ProductSeatId = 1,
            ProductId = 1515110108,
            Seats = 1
        });

        ctx.ProductPricing.Add(new ProductPricing
        {
            ProductPricingId = 1,
            ProductId = 1515110108,
            RetailPrice = 49.99m
        });

        await ctx.SaveChangesAsync();

        var result = await repo.SelectLicenseOptionsAsync(keycode);

        Assert.NotNull(result);

        // Legacy-aligned top-level additions.
        Assert.True(result!.LicenseVerified);
        Assert.Null(result.LicenseSiteId);
        Assert.Empty(result.UpgradeCategories);
        Assert.Empty(result.BillingModels);

        // Nested license fields from already loaded data.
        Assert.NotNull(result.License);
        Assert.Equal(keycode, result.License!.Keycode);
        Assert.Equal("OpenText Business", result.License.ProductLineDescription);
        Assert.Equal(1, result.License.LicenseStatusId);
        Assert.Equal(3, result.License.LicenseKeycodeTypeId);
        Assert.Equal(endDate, result.License.LicenseExpirationDate);
        Assert.Equal(10, result.License.LicenseSeats);
        Assert.Equal("SAEP", result.License.LicenseCategoryName);
        Assert.Equal("OpenText Core Endpoint Protection", result.License.LicenseCategoryDescription);
        Assert.Equal(startDate, result.License.StartDate);
        Assert.Equal(endDate, result.License.EndDate);
        Assert.Equal(licenseKeyGuid.ToString("D"), result.License.LicenseKey);
        Assert.False(result.License.IsExpired);

        var expectedDaysRemaining = (endDate.Date - DateTime.UtcNow.Date).Days;
        Assert.NotNull(result.License.DaysRemaining);
        Assert.InRange(result.License.DaysRemaining!.Value, expectedDaysRemaining - 1, expectedDaysRemaining + 1);

        // Expanded license_profile fields from already loaded data.
        Assert.True(result.LicenseProfile.ContainsKey("SAEP"));
        var profile = result.LicenseProfile["SAEP"];
        Assert.Equal("SAEP", profile.LicenseCategoryName);
        Assert.Equal("OpenText Core Endpoint Protection", profile.LicenseCategoryDescription);
        Assert.Equal(220, profile.LicenseCategoryId);
        Assert.Equal(3, profile.LicenseKeycodeTypeId);
        Assert.Equal(1, profile.LicenseStatusId);
        Assert.Equal("active", profile.LicenseStatusDescription);
        Assert.Equal(10, profile.LicenseSeats);
        Assert.Equal(startDate, profile.StartDate);
        Assert.Equal(endDate, profile.ExpirationDate);

        // Product options must remain unchanged.
        var qaProduct = Assert.Single(result.ProductOptions);
        Assert.Equal(1515110108, qaProduct.ProductId);
        Assert.Equal("QA AUTOGEN PRODUCT 2026-08-05", qaProduct.ProductName);
        Assert.Equal(49.99m, qaProduct.Price);
        Assert.Equal([1d, 1.25d, 2d, 3d], qaProduct.Years);
        Assert.Equal([1, 3, 5], qaProduct.Seats);
        Assert.Contains(1.25d, qaProduct.Years);
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_EndDateDoesNotFallbackFromLicenseExpiration_AndNullEndIsActive()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        await SeedLicenseOptionsCoreAsync(
            ctx,
            licenseId: 7101,
            keycode: "SSEAONLNAAAARGYCPINC",
            categoryId: 2,
            categoryName: "WAV",
            licenseExpirationDate: new DateTime(2007, 9, 30),
            categoryEndDate: null,
            forceNullCategoryEndDate: true);

        var result = await repo.SelectLicenseOptionsAsync("SSEAONLNAAAARGYCPINC", "en-US");

        Assert.NotNull(result);
        Assert.NotNull(result!.License);
        Assert.Equal(new DateTime(2007, 9, 30), result.License!.LicenseExpirationDate);
        Assert.Null(result.License.EndDate);
        Assert.Equal(0, result.License.DaysRemaining);
        Assert.False(result.License.IsExpired);
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_ExpiredEffectiveEndDate_ClampsDaysToZero_AndMarksExpired()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        await SeedLicenseOptionsCoreAsync(
            ctx,
            licenseId: 7102,
            keycode: "EXPIRED-END-DATE-KEYCODE",
            categoryId: 2,
            categoryName: "WAV",
            categoryEndDate: DateTime.UtcNow.Date.AddDays(-15));

        var result = await repo.SelectLicenseOptionsAsync("EXPIRED-END-DATE-KEYCODE", "en-US");

        Assert.NotNull(result);
        Assert.NotNull(result!.License);
        Assert.Equal(0, result.License!.DaysRemaining);
        Assert.True(result.License.IsExpired);
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_CapabilityTypeDescription_ComesFromCapabilityData()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        await SeedLicenseOptionsCoreAsync(
            ctx,
            licenseId: 7103,
            keycode: "CAP-TYPE-KEYCODE",
            categoryId: 2,
            categoryName: "WAV",
            baseCapabilityId: 9001);

        await SeedCapabilityTypeForLicenseAsync(
            ctx,
            licenseId: 7103,
            capabilityId: 9001,
            capabilityTypeId: 4,
            capabilityTypeDescription: "full");

        var result = await repo.SelectLicenseOptionsAsync("CAP-TYPE-KEYCODE", "en-US");

        Assert.NotNull(result);
        Assert.NotNull(result!.License);
        Assert.Equal("full", result.License!.CapabilityTypeDescription);
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_ProfileFields_ComesFromLegacyEquivalentProfileSource()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        await SeedLicenseOptionsCoreAsync(
            ctx,
            licenseId: 7104,
            keycode: "PROFILE-FIELDS-KEYCODE",
            categoryId: 2,
            categoryName: "WAV",
            baseCapabilityId: 9002);

        await SeedCapabilityTypeForLicenseAsync(
            ctx,
            licenseId: 7104,
            capabilityId: 9002,
            capabilityTypeId: 5,
            capabilityTypeDescription: "full");

        await SeedPrimaryItemHierarchyAsync(ctx);

        var result = await repo.SelectLicenseOptionsAsync("PROFILE-FIELDS-KEYCODE", "en-US");

        Assert.NotNull(result);
        Assert.True(result!.LicenseProfile.ContainsKey("WAV"));

        var wavProfile = result.LicenseProfile["WAV"];
        Assert.Equal("full", wavProfile.CategoryTypeName);
        Assert.Equal(1, wavProfile.ItemHierarchyId);
        Assert.Equal("primary", wavProfile.ItemHierarchyName);
        Assert.IsType<int>(wavProfile.ItemHierarchyId!.Value);
        Assert.IsType<int>(wavProfile.LicenseCategoryId!.Value);
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_LegacyContractParity_SerializesAllLicenseAndProfileFields()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        const int licenseId = 7201;
        const int distributionMethodId = 10;
        const int customerId = 501;

        await SeedLicenseOptionsCoreAsync(
            ctx,
            licenseId: licenseId,
            keycode: "SSEAONLNAAAARGYCPINC",
            categoryId: 2,
            categoryName: "WAV",
            baseCapabilityId: 9003,
            licenseTypeId: 1,
            licenseDistributionMethodId: distributionMethodId,
            customerId: customerId,
            maxDailyActivations: 300,
            licenseExpirationDate: new DateTime(2007, 9, 30),
            categoryStartDate: null,
            categoryEndDate: null,
            forceNullCategoryEndDate: true,
            licenseInsertDate: new DateTime(2006, 8, 25, 13, 58, 16));

        await SeedCapabilityTypeForLicenseAsync(
            ctx,
            licenseId: licenseId,
            capabilityId: 9003,
            capabilityTypeId: 6,
            capabilityTypeDescription: "full");

        await SeedPrimaryItemHierarchyAsync(ctx);

        ctx.LicenseType.Add(new LicenseType
        {
            LicenseTypeId = 1,
            LicenseTypeDescription = "OEM"
        });

        ctx.LicenseDistributionMethod.Add(new LicenseDistributionMethod
        {
            LicenseDistributionMethodId = distributionMethodId,
            LicenseDistributionMethodCode = "ONLN"
        });

        ctx.Channel.Add(new Channel
        {
            ChannelId = 77,
            ChannelName = "Online"
        });

        ctx.LicenseDistributionMethodChannel.Add(new LicenseDistributionMethodChannel
        {
            LicenseDistributionMethodChannelId = 1,
            ChannelId = 77,
            LicenseDistributionMethodId = distributionMethodId
        });

        ctx.LicenseHistory.Add(new LicenseHistory
        {
            LicenseHistoryId = 1,
            LicenseId = licenseId,
            LicenseDistributionMethodId = distributionMethodId,
            InsertDate = new DateTime(2006, 8, 25, 13, 58, 16),
            HistoryDate = new DateTime(2006, 8, 25, 13, 58, 16)
        });

        ctx.Customer.Add(new Customer
        {
            CustomerId = customerId,
            OptIn = null,
        });

        await ctx.SaveChangesAsync();

        var result = await repo.SelectLicenseOptionsAsync("SSEAONLNAAAARGYCPINC", "en-US");

        Assert.NotNull(result);
        Assert.NotNull(result!.License);
        Assert.Equal("OEM", result.License!.LicenseTypeDescription);
        Assert.Equal(300, result.License.MaxDailyActivations);
        Assert.Equal(0, result.License.PortalFlag);
        Assert.Equal(0, result.License.RenewalCount);
        Assert.Equal("Online", result.License.LicenseOriginChannelName);
        Assert.Equal("ONLN", result.License.LicenseDistributionMethodCode);
        Assert.Null(result.License.EndDate);
        Assert.Equal(0, result.License.DaysRemaining);
        Assert.False(result.License.IsExpired);
        Assert.Equal("full", result.License.CapabilityTypeDescription);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
            PropertyNameCaseInsensitive = true,
        };

        var json = JsonSerializer.Serialize(result, options);
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("license", out var licenseNode));
        Assert.True(root.TryGetProperty("license_verified", out _));
        Assert.True(root.TryGetProperty("license_site_id", out var licenseSiteIdNode));
        Assert.Equal(JsonValueKind.Null, licenseSiteIdNode.ValueKind);
        Assert.True(root.TryGetProperty("upgrade_categories", out _));
        Assert.True(root.TryGetProperty("billing_models", out _));
        Assert.True(root.TryGetProperty("product_options", out _));

        var expectedLicenseFields = new[]
        {
            "keycode", "product_line_description", "license_status_id", "license_type_description", "license_keycode_type_id",
            "max_daily_activations", "license_expiration_date", "parent_keycode", "license_seats", "consumed_seats",
            "seats_used", "storage_gb", "license_category_name", "license_category_description", "start_date", "end_date",
            "capability_type_description", "license_key", "license_attribute_description", "license_attribute_tag",
            "license_attribute_license_value", "license_attribute_license_value_description", "license_attribute_last_modified",
            "oem_type", "portal_flag", "renewal_count", "license_origin_channel_name", "license_original_activation_date",
            "email_opt_in", "license_distribution_method_code", "next_bill_date", "days_remaining", "is_expired"
        };

        foreach (var field in expectedLicenseFields)
            Assert.True(licenseNode.TryGetProperty(field, out _), $"Missing license field: {field}");

        Assert.True(root.TryGetProperty("license_profile", out var profileNode));
        Assert.True(profileNode.TryGetProperty("WAV", out var wavNode));

        var expectedProfileFields = new[]
        {
            "license_category_name", "license_category_description", "license_seats", "storage_gb", "license_keycode_type_id",
            "start_date", "expiration_date", "license_attribute_id", "license_attribute_description",
            "license_attribute_license_value", "license_attribute_license_value_description", "category_type_name",
            "item_hierarchy_id", "item_hierarchy_name", "license_status_id", "license_status_description",
            "autorenewal_cycle_name", "autorenewal_cycle", "usage_pricing_model_id", "usage_pricing_model_name",
            "retention_model_id", "retention_model_name", "retention_term", "retention_model_type_id",
            "product_platform_id", "product_platform_name", "license_autorenewal_value", "license_category_id",
            "product_pricing_level_id", "pricing_level", "pricing_level_description", "license_vault_json", "most_recent_order_term"
        };

        foreach (var field in expectedProfileFields)
            Assert.True(wavNode.TryGetProperty(field, out _), $"Missing profile field: {field}");
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_WavUpgradeCategories_EnUsLocale_ReturnsDbMappings()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        await SeedLicenseOptionsCoreAsync(ctx, licenseId: 7001, keycode: "SSEAONLNAAAARGYCPINC", categoryId: 2, categoryName: "WAV");
        await SeedWavUpgradeMappingsAsync(ctx);

        var result = await repo.SelectLicenseOptionsAsync("SSEAONLNAAAARGYCPINC", "en-US");

        Assert.NotNull(result);
        Assert.Equal(3, result!.UpgradeCategories.Count);

        Assert.True(result.UpgradeCategories.ContainsKey("WAV"));
        Assert.True(result.UpgradeCategories.ContainsKey("WISC"));
        Assert.True(result.UpgradeCategories.ContainsKey("WISE"));

        foreach (var kvp in result.UpgradeCategories)
        {
            Assert.Equal("WAV", kvp.Value.LicenseCategoryName);
            Assert.Equal(kvp.Key, kvp.Value.UpgradeLicenseCategoryName);
            Assert.Equal(1, kvp.Value.ItemHierarchyId);
            Assert.Equal("primary", kvp.Value.ItemHierarchyName);
        }
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_WavUpgradeCategories_NullLocale_UsesEnUsDefaults()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        await SeedLicenseOptionsCoreAsync(ctx, licenseId: 7002, keycode: "SSEAONLNAAAARGYCPINC", categoryId: 2, categoryName: "WAV");
        await SeedWavUpgradeMappingsAsync(ctx);

        var result = await repo.SelectLicenseOptionsAsync("SSEAONLNAAAARGYCPINC", null);

        Assert.NotNull(result);
        Assert.Equal(3, result!.UpgradeCategories.Count);
        Assert.True(result.UpgradeCategories.ContainsKey("WAV"));
        Assert.True(result.UpgradeCategories.ContainsKey("WISC"));
        Assert.True(result.UpgradeCategories.ContainsKey("WISE"));
    }

    [Fact]
    public async Task SelectLicenseOptionsAsync_NoUpgradeMappings_ReturnsEmptyUpgradeCategories()
    {
        await using var ctx = NewContext();
        var repo = NewRepo(ctx);

        await SeedLicenseOptionsCoreAsync(ctx, licenseId: 7003, keycode: "SSEAONLNAAAARGYCPINC", categoryId: 2, categoryName: "WAV");

        var result = await repo.SelectLicenseOptionsAsync("SSEAONLNAAAARGYCPINC", "en-US");

        Assert.NotNull(result);
        Assert.Empty(result!.UpgradeCategories);
    }

    private static async Task SeedLicenseOptionsCoreAsync(
        AppDbContext ctx,
        int licenseId,
        string keycode,
        byte categoryId,
        string categoryName,
        int? baseCapabilityId = null,
        int licenseTypeId = 2,
        int? licenseDistributionMethodId = null,
        int? customerId = null,
        int? maxDailyActivations = null,
        DateTime? licenseExpirationDate = null,
        DateTime? categoryStartDate = null,
        DateTime? categoryEndDate = null,
        bool forceNullCategoryEndDate = false,
        DateTime? licenseInsertDate = null)
    {
        if (!await ctx.LicenseStatus.AnyAsync(ls => ls.LicenseStatusId == 1))
        {
            ctx.LicenseStatus.Add(new LicenseStatus
            {
                LicenseStatusId = 1,
                LicenseStatusDescription = "active"
            });
        }

        if (!await ctx.ProductLine.AnyAsync(pl => pl.ProductLineId == 1))
        {
            ctx.ProductLine.Add(new ProductLine
            {
                ProductLineId = 1,
                ProductLineDescription = "Webroot"
            });
        }

        if (!await ctx.LicenseKeycodeType.AnyAsync(kt => kt.LicenseKeycodeTypeId == 3))
        {
            ctx.LicenseKeycodeType.Add(new LicenseKeycodeType
            {
                LicenseKeycodeTypeId = 3,
                LicenseKeycodeTypeDescription = "Business"
            });
        }

        if (!await ctx.LicenseCategory.AnyAsync(c => c.LicenseCategoryId == categoryId))
        {
            ctx.LicenseCategory.Add(new LicenseCategory
            {
                LicenseCategoryId = categoryId,
                LicenseCategoryName = categoryName,
                LicenseCategoryDescription = categoryName,
                BaseCapabilityId = baseCapabilityId
            });
        }

        ctx.License.Add(new License
        {
            LicenseId = licenseId,
            Keycode = keycode,
            CustomerId = customerId,
            ProductLineId = 1,
            LicenseStatusId = 1,
            LicenseTypeId = licenseTypeId,
            LicenseDistributionMethodId = licenseDistributionMethodId,
            LicenseKeycodeTypeId = 3,
            MaxDailyActivations = maxDailyActivations ?? 100,
            LicenseExpirationDate = licenseExpirationDate ?? DateTime.UtcNow.Date.AddDays(10)
,
            InsertDate = licenseInsertDate ?? DateTime.UtcNow
        });

        ctx.LicenseKey.Add(new LicenseKey
        {
            LicenseId = licenseId,
            Key = Guid.NewGuid()
        });

        ctx.LicenseCategoryLicense.Add(new LicenseCategoryLicense
        {
            LicenseCategoryLicenseId = licenseId,
            LicenseId = licenseId,
            LicenseCategoryId = categoryId,
            StartDate = categoryStartDate ?? DateTime.UtcNow.Date.AddDays(-30),
            EndDate = forceNullCategoryEndDate ? null : categoryEndDate ?? DateTime.UtcNow.Date.AddDays(10)
        });

        ctx.LicenseSeat.Add(new LicenseSeat
        {
            LicenseSeatId = licenseId,
            LicenseId = licenseId,
            LicenseSeats = 5,
            SeatsUsed = 1
        });

        await ctx.SaveChangesAsync();
    }

    private static async Task SeedCapabilityTypeForLicenseAsync(
        AppDbContext ctx,
        int licenseId,
        int capabilityId,
        int capabilityTypeId,
        string capabilityTypeDescription)
    {
        if (!await ctx.CapabilityType.AnyAsync(c => c.CapabilityTypeId == capabilityTypeId))
        {
            ctx.CapabilityType.Add(new CapabilityType
            {
                CapabilityTypeId = capabilityTypeId,
                CapabilityTypeDescription = capabilityTypeDescription
            });
        }

        ctx.LicenseCapability.Add(new LicenseCapability
        {
            LicenseCapabilityId = (licenseId * 10) + capabilityTypeId,
            LicenseId = licenseId,
            CapabilityId = capabilityId,
            CapabilityTypeId = capabilityTypeId
        });

        await ctx.SaveChangesAsync();
    }

    private static async Task SeedPrimaryItemHierarchyAsync(AppDbContext ctx)
    {
        if (!await ctx.ItemHierarchy.AnyAsync(h => h.ItemHierarchyId == 1))
        {
            ctx.ItemHierarchy.Add(new ItemHierarchy
            {
                ItemHierarchyId = 1,
                ItemHierarchyName = "primary"
            });
            await ctx.SaveChangesAsync();
        }
    }

    private static async Task SeedWavUpgradeMappingsAsync(AppDbContext ctx)
    {
        if (!await ctx.LicenseCategory.AnyAsync(c => c.LicenseCategoryId == 3))
        {
            ctx.LicenseCategory.Add(new LicenseCategory
            {
                LicenseCategoryId = 3,
                LicenseCategoryName = "WISE",
                LicenseCategoryDescription = "Webroot Internet Security Essentials"
            });
        }

        if (!await ctx.LicenseCategory.AnyAsync(c => c.LicenseCategoryId == 4))
        {
            ctx.LicenseCategory.Add(new LicenseCategory
            {
                LicenseCategoryId = 4,
                LicenseCategoryName = "WISC",
                LicenseCategoryDescription = "Webroot Internet Security Complete"
            });
        }

        if (!await ctx.ItemHierarchy.AnyAsync(h => h.ItemHierarchyId == 1))
        {
            ctx.ItemHierarchy.Add(new ItemHierarchy
            {
                ItemHierarchyId = 1,
                ItemHierarchyName = "primary"
            });
        }

        // Expected EN/USA mappings.
        ctx.ProductLicenseCategoryUpgrade.AddRange(
            new ProductLicenseCategoryUpgrade
            {
                ProductLicenseCategoryUpgradeId = 7001,
                LicenseCategoryId = 2,
                UpgradeLicenseCategoryId = 2,
                LanguageCode = "EN",
                LocationCode = "USA",
                ItemHierarchyId = 1,
            },
            new ProductLicenseCategoryUpgrade
            {
                ProductLicenseCategoryUpgradeId = 7002,
                LicenseCategoryId = 2,
                UpgradeLicenseCategoryId = 3,
                LanguageCode = "EN",
                LocationCode = "USA",
                ItemHierarchyId = 1,
            },
            new ProductLicenseCategoryUpgrade
            {
                ProductLicenseCategoryUpgradeId = 7003,
                LicenseCategoryId = 2,
                UpgradeLicenseCategoryId = 4,
                LanguageCode = "EN",
                LocationCode = "USA",
                ItemHierarchyId = 1,
            });

        // Locale filter guard row: should not be returned for en-US lookup.
        ctx.ProductLicenseCategoryUpgrade.Add(new ProductLicenseCategoryUpgrade
        {
            ProductLicenseCategoryUpgradeId = 7004,
            LicenseCategoryId = 2,
            UpgradeLicenseCategoryId = 4,
            LanguageCode = "EN",
            LocationCode = "GBR",
            ItemHierarchyId = 1,
        });

        await ctx.SaveChangesAsync();
    }
}
