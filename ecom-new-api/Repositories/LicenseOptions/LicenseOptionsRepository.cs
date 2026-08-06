using ecom_new_api.Data;
using ecom_new_api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories.LicenseOptions;

public sealed class LicenseOptionsRepository : ILicenseOptionsRepository
{
    private readonly AppDbContext _db;

    public LicenseOptionsRepository(AppDbContext db) => _db = db;

    public async Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode,
        CancellationToken ct = default)
    {
        var license = await (
            from l in _db.License
            join s in _db.LicenseStatus on l.LicenseStatusId equals s.LicenseStatusId
            join pl in _db.ProductLine on l.ProductLineId equals pl.ProductLineId
            join lkRow in _db.LicenseKey on l.LicenseId equals lkRow.LicenseId into lkJoin
            from lkRow in lkJoin.DefaultIfEmpty()
            where l.Keycode == keycode
            select new
            {
                l.LicenseId,
                l.Keycode,
                l.LicenseExpirationDate,
                StatusDescription = s.LicenseStatusDescription,
                ProductLineDescription = pl.ProductLineDescription,
                LicenseKeyGuid = lkRow == null ? (Guid?)null : (Guid?)lkRow.Key
            }
        ).FirstOrDefaultAsync(ct);

        if (license is null) return null;

        var categoryRows = await (
            from lcl in _db.LicenseCategoryLicense
            join lc in _db.LicenseCategory on lcl.LicenseCategoryId equals lc.LicenseCategoryId
            where lcl.LicenseId == license.LicenseId
            orderby lcl.LicenseCategoryLicenseId descending
            select new
            {
                lc.LicenseCategoryId,
                lc.LicenseCategoryName,
                lc.LicenseCategoryDescription,
                lcl.StartDate,
                EndDate = lcl.EndDate
            }
        ).ToListAsync(ct);

        var primaryCategory = categoryRows.FirstOrDefault();

        var seats = await _db.LicenseSeat
            .Where(ls => ls.LicenseId == license.LicenseId)
            .OrderByDescending(ls => ls.LicenseSeatId)
            .Select(ls => (int?)ls.LicenseSeats)
            .FirstOrDefaultAsync(ct);

        List<ProductOptionResponse> productOptions = [];
        if (primaryCategory is not null)
        {
            var products = await (
                from plc in _db.ProductLicenseCategory
                join p in _db.Product on plc.ProductId equals p.ProductId
                join pt in _db.ProductType on p.ProductTypeId equals pt.ProductTypeId
                where plc.LicenseCategoryId == primaryCategory.LicenseCategoryId
                   && (p.ProductTypeId == 1 || p.ProductTypeId == 2)
                select new { p.ProductId, ProductName = p.ProductDescription, TypeDescription = pt.ProductTypeDescription }
            ).ToListAsync(ct);

            if (products.Count > 0)
            {
                var productIds = products.Select(p => p.ProductId).ToList();

                var allYears = await _db.ProductYears
                    .Where(py => productIds.Contains(py.ProductId))
                    .Select(py => new { py.ProductId, py.Years })
                    .ToListAsync(ct);

                var allSeats = await _db.ProductSeat
                    .Where(ps => productIds.Contains(ps.ProductId))
                    .Select(ps => new { ps.ProductId, ps.Seats })
                    .ToListAsync(ct);

                var allPricing = await _db.ProductPricing
                    .Where(pp => productIds.Contains(pp.ProductId))
                    .Select(pp => new { pp.ProductId, pp.RetailPrice })
                    .ToListAsync(ct);

                productOptions = products.Select(p => new ProductOptionResponse
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName ?? string.Empty,
                    LicenseCategoryName = primaryCategory.LicenseCategoryName,
                    ProductTypeDescription = p.TypeDescription,
                    Price = allPricing.FirstOrDefault(pp => pp.ProductId == p.ProductId)?.RetailPrice,
                    Years = allYears.Where(py => py.ProductId == p.ProductId).Select(py => py.Years).ToList(),
                    Seats = allSeats.Where(ps => ps.ProductId == p.ProductId).Select(ps => ps.Seats).ToList(),
                }).ToList();
            }
        }

        var licenseProfile = categoryRows.ToDictionary(
            row => row.LicenseCategoryName,
            row => new LicenseProfileEntryResponse
            {
                LicenseCategoryName = row.LicenseCategoryName,
                LicenseCategoryDescription = row.LicenseCategoryDescription,
                StartDate = row.StartDate,
                ExpirationDate = row.EndDate,
                LicenseSeats = seats,
                CategoryTypeName = null,
            });

        var licenseInfo = new LicenseInfoResponse
        {
            Keycode = license.Keycode,
            LicenseKey = license.LicenseKeyGuid?.ToString("D"),
            LicenseCategoryName = primaryCategory?.LicenseCategoryName,
            LicenseSeats = seats,
        };

        return new LicenseOptionsResponse
        {
            Keycode = license.Keycode,
            LicenseKey = license.LicenseKeyGuid?.ToString("D"),
            LicenseStatus = license.StatusDescription,
            ProductLine = license.ProductLineDescription,
            LicenseCategory = primaryCategory?.LicenseCategoryName,
            LicenseCategoryDescription = primaryCategory?.LicenseCategoryDescription,
            LicenseSeats = seats,
            ExpirationDate = license.LicenseExpirationDate,
            ProductOptions = productOptions,
            License = licenseInfo,
            LicenseProfile = licenseProfile,
        };
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
