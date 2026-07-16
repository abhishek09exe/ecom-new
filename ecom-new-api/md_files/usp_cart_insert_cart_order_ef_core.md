# EF Core conversion for usp_cart_insert_cart_order

This is a core-logic EF Core translation of the SQL procedure.  
Assumptions:
- `AppDbContext` is already configured and injected.
- Entity mappings exist for all referenced tables.
- A helper exists for vendor order code generation when not supplied.

## Suggested request model

```csharp
public sealed class CartOrderCreateInput
{
    public string SiteId { get; init; } = default!;
    public string Locale { get; init; } = default!;
    public string UserIp { get; init; } = default!;
    public string? CartExtensionJson { get; init; }
}
```

## Optional extension payload model

```csharp
public sealed class CartExtensionData
{
    public DateTime? SalesOrderDate { get; set; }
    public string? CurrencyCode { get; set; }
    public string? VendorOrderCode { get; set; }
    public string? PartnerKey { get; set; }
    public string? AccountUserName { get; set; }
    public string? RoutingAction { get; set; }
    public int? MessageCampaignId { get; set; }
    public string? MessageCampaignPlatform { get; set; }
    public string? Key { get; set; }
    public int? CartDiscountId { get; set; }
}
```

## Core EF Core logic

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public sealed class CartOrderWriter
{
    private readonly AppDbContext _db;

    public CartOrderWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CartOrder> CreateCartOrderAsync(CartOrderCreateInput input, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var now = DateTime.UtcNow;
            var ext = ParseExtension(input.CartExtensionJson);

            var salesOrderDate = ext?.SalesOrderDate?.Date ?? now.Date;

            // 1) Partner lookup by partner_key
            int? partnerId = null;
            if (!string.IsNullOrWhiteSpace(ext?.PartnerKey))
            {
                partnerId = await _db.Partner
                    .Where(p => p.PartnerKey == ext!.PartnerKey)
                    .Select(p => (int?)p.PartnerId)
                    .SingleOrDefaultAsync(ct);
            }

            // 2) Currency resolution
            byte? currencyId = null;

            if (!string.IsNullOrWhiteSpace(ext?.CurrencyCode))
            {
                currencyId = await _db.Currency
                    .Where(c => c.CurrencyCode == ext!.CurrencyCode)
                    .Select(c => (byte?)c.CurrencyId)
                    .SingleOrDefaultAsync(ct);
            }

            // fallback to partner configuration (partner_configuration_id = 15)
            if (currencyId is null && partnerId is not null)
            {
                var partnerCurrencyCode = await _db.PartnerConfigurationPartner
                    .Where(cp => cp.PartnerId == partnerId && cp.PartnerConfigurationId == 15)
                    .Select(cp => cp.ConfigurationValue)
                    .SingleOrDefaultAsync(ct);

                if (!string.IsNullOrWhiteSpace(partnerCurrencyCode))
                {
                    currencyId = await _db.Currency
                        .Where(c => c.CurrencyCode == partnerCurrencyCode)
                        .Select(c => (byte?)c.CurrencyId)
                        .SingleOrDefaultAsync(ct);
                }
            }

            currencyId ??= 1; // procedure default

            // 3) Vendor order code generation
            var vendorOrderCode = ext?.VendorOrderCode;
            if (string.IsNullOrWhiteSpace(vendorOrderCode))
            {
                var prefix = await _db.CartSiteIdOrderCodePrefix
                    .Where(x => x.SiteId == input.SiteId)
                    .Select(x => x.VendorOrderCodePrefix)
                    .SingleOrDefaultAsync(ct) ?? string.Empty;

                var invoiceCode = await GetNextInvoiceCodeAsync(ct); // equivalent to usp_next_id(Type=3)
                vendorOrderCode = $"{prefix}{invoiceCode}";
            }

            // 4) Insert cart_order
            var order = new CartOrder
            {
                VendorOrderCode = vendorOrderCode!,
                OrderType = input.SiteId,
                SiteId = input.SiteId,
                SiteUrl = input.SiteId,
                SalesOrderDate = salesOrderDate,
                SubmissionDate = now,
                Locale = input.Locale,
                UserIp = input.UserIp,
                CurrencyId = currencyId.Value,
                InsertDate = now
            };

            _db.CartOrder.Add(order);
            await _db.SaveChangesAsync(ct); // obtains CartOrderId

            // 5) Insert cart_order_partner (optional)
            if (partnerId is not null)
            {
                int? partnerAccountId = null;

                if (!string.IsNullOrWhiteSpace(ext?.AccountUserName))
                {
                    partnerAccountId = await (
                        from p in _db.PartnerAccount
                        join a in _db.Account on p.AccountId equals a.AccountId
                        where p.PartnerId == partnerId && a.AccountUserName == ext.AccountUserName
                        select (int?)p.PartnerAccountId
                    ).SingleOrDefaultAsync(ct);
                }

                _db.CartOrderPartner.Add(new CartOrderPartner
                {
                    CartOrderId = order.CartOrderId,
                    PartnerId = partnerId.Value,
                    PartnerAccountId = partnerAccountId
                });
            }

            // 6) Insert cart_order_route (optional)
            if (!string.IsNullOrWhiteSpace(ext?.RoutingAction))
            {
                _db.CartOrderRoute.Add(new CartOrderRoute
                {
                    CartOrderId = order.CartOrderId,
                    RoutingAction = ext.RoutingAction,
                    InsertDate = now
                });
            }

            // 7) Insert cart_order_message (optional)
            if (!string.IsNullOrWhiteSpace(ext?.Key))
            {
                var licenseId = await _db.LicenseKey
                    .Where(k => k.Key == ext.Key)
                    .Select(k => (int?)k.LicenseId)
                    .SingleOrDefaultAsync(ct);

                _db.CartOrderMessage.Add(new CartOrderMessage
                {
                    CartOrderId = order.CartOrderId,
                    MessageKey = ext.Key,
                    MessageCampaignId = ext.MessageCampaignId,
                    MessageCampaignPlatform = ext.MessageCampaignPlatform,
                    CartDiscountId = ext.CartDiscountId,
                    LicenseId = licenseId
                });
            }

            // 8) Insert cart_json (optional)
            if (!string.IsNullOrWhiteSpace(input.CartExtensionJson))
            {
                _db.CartJson.Add(new CartJson
                {
                    CartOrderId = order.CartOrderId,
                    Json = input.CartExtensionJson
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Equivalent to response_code = 0 + selecting inserted cart_order
            return order;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw; // map to your API error contract (response_code = -200) at service/controller boundary
        }
    }

    private static CartExtensionData? ParseExtension(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        var data = JsonSerializer.Deserialize<CartExtensionData>(json);

        // Match SQL behavior where empty key is treated as null
        if (data is not null && string.IsNullOrWhiteSpace(data.Key))
        {
            data.Key = null;
        }

        return data;
    }

    private async Task<int> GetNextInvoiceCodeAsync(CancellationToken ct)
    {
        // Keep this implementation aligned with your existing ID strategy.
        // Example choices:
        // 1) SQL sequence via FromSql/ExecuteSql
        // 2) Dedicated key table
        // 3) Wrapper around usp_next_id
        throw new NotImplementedException();
    }
}
```

## Mapping notes from procedure to EF Core

- `openjson(@cart_extension_json)` -> `JsonSerializer.Deserialize<CartExtensionData>`.
- Partner lookup by `partner_key` preserved.
- Currency fallback order preserved:
  1. JSON `currency_code`
  2. partner configuration with id `15`
  3. hard default `currency_id = 1`
- Optional inserts are preserved for:
  - `cart_order_partner`
  - `cart_order_route`
  - `cart_order_message`
  - `cart_json`
- SQL `TRY/CATCH` -> C# `try/catch` with transaction rollback.
