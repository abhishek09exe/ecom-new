using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories.Trials;

public sealed class LicenseServiceStagingRepository(AppDbContext db)
{
    public async Task<int> StageLicenseCreateAsync(string serviceJson, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(serviceJson) || serviceJson == "[]")
            throw new InvalidOperationException("License-create service JSON is required and cannot be an empty array.");

        var type = await db.LicenseServiceType.AsNoTracking()
            .SingleOrDefaultAsync(type => type.LicenseServiceTypeName == "license_create", ct);
        if (type is null || type.LicenseServiceTypeId != 20 || type.ProcessType != "synchronous")
            throw new InvalidOperationException("license_create must be configured as synchronous service type 20.");

        var status = await db.LicenseServiceStatus.AsNoTracking()
            .SingleOrDefaultAsync(status => status.LicenseServiceStatusName.ToUpper() == "PENDING", ct)
            ?? throw new InvalidOperationException("Pending license-service status is not configured.");

        var service = new LicenseService
        {
            LicenseServiceTypeId = type.LicenseServiceTypeId,
            LicenseServiceStatusId = status.LicenseServiceStatusId
        };
        var serviceJsonRow = new LicenseServiceJson
        {
            LicenseServiceId = 0,
            ServiceJson = serviceJson
        };

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.LicenseService.Add(service);
            await db.SaveChangesAsync(ct);

            serviceJsonRow.LicenseServiceId = service.LicenseServiceId;
            db.LicenseServiceJson.Add(serviceJsonRow);
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            db.Entry(service).State = EntityState.Detached;
            return service.LicenseServiceId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.Entry(service).State = EntityState.Detached;
            if (db.Entry(serviceJsonRow).State != EntityState.Detached)
                db.Entry(serviceJsonRow).State = EntityState.Detached;
            throw;
        }
    }
}