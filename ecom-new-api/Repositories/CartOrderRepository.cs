using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Data;
using Microsoft.EntityFrameworkCore;
using ecom_new_api.Data.Entities;

namespace ecom_new_api.Repositories;

public class CartOrderRepository : ICartOrderRepository
{
    private readonly AppDbContext _context;

    public CartOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request,
        CancellationToken ct = default)
    {
        var vendorOrderCode = "POC-" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        var cartOrder = new CartOrder
        {
            VendorOrderCode = vendorOrderCode,
            OrderType = request.SiteId,
            SiteId = request.SiteId,
            SiteUrl = request.SiteId,

            SalesOrderDate = DateTime.UtcNow,
            SubmissionDate = DateTime.UtcNow,

            Locale = request.Locale,
            UserIp = request.UserIp,

            CurrencyId = 1,
            InsertDate = DateTime.UtcNow,

            InsertBy = "POC",
            ModifiedBy = "POC",
            ModifiedDate = DateTime.UtcNow,

            CartOrderStatusId = 1
        };

        _context.CartOrders.Add(cartOrder);

        await _context.SaveChangesAsync(ct);

        return vendorOrderCode;
    }

    public async Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode,
        CancellationToken ct = default)
    {
        return await _context.CartOrders
            .Where(co => co.VendorOrderCode == vendorOrderCode)
            .Select(co => new CartOrderResponse
            {
                CartOrderId = co.CartOrderId,
                VendorOrderCode = co.VendorOrderCode!,
                SiteId = co.SiteId,
                OfferAmount = co.OfferAmount,
                TotalAmount = co.TotalAmount,
                SubTotalAmount = co.SubTotalAmount,
                TaxAmount = co.TaxAmount,
                SalesOrderDate = co.SalesOrderDate ?? DateTime.MinValue,
                Locale = co.Locale,
                InsertDate = co.InsertDate,
                InsertBy = co.InsertBy,
                ModifiedDate = co.ModifiedDate,
                ModifiedBy = co.ModifiedBy,
                CartOrderStatusId = co.CartOrderStatusId,
                UserIp = co.UserIp,

                CurrencyId = co.Currency != null ? co.Currency.CurrencyId : 0,
                CurrencyCode = co.Currency != null ? (co.Currency.CurrencyCode ?? string.Empty) : string.Empty,

                PartnerKey = co.CartOrderPartners
                    .Select(cp => cp.Partner.PartnerKey.ToString())
                    .FirstOrDefault(),

                CartJson = co.CartJson != null
                    ? co.CartJson.Json
                    : null,

                // SP2 will populate this later
                Items = new List<CartOrderItemResponse>()
            })
            .FirstOrDefaultAsync(ct);
    }
    public Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ConfigureResponse?> SelectConfigureAsync(
        string keycode,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<UpgradeResponse?> SelectUpgradeAsync(
        string keycode,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}