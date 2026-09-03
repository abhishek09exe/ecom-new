using System.Data;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Observability;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace ecom_new_api.Repositories.Pricing;

public sealed class PricingRepository : IPricingRepository
{
    private const string ProcName = "usp_cart_select_license_configurator_pricing";

    private readonly AppDbContext _ctx;
    private readonly ILogger<PricingRepository> _logger;

    public PricingRepository(AppDbContext ctx, ILogger<PricingRepository> logger)
        => (_ctx, _logger) = (ctx, logger);

    public async Task<List<ConfiguratorPricingResult>> GetConfiguratorPricingAsync(
        string itemJson, string bundleJson)
    {
        // Explicit SqlParameter required; string defaults to NVARCHAR(4000) and can truncate large JSON payloads.
        var p1 = new SqlParameter("@item_json", SqlDbType.NVarChar, -1) { Value = itemJson };
        var p2 = new SqlParameter("@bundle_json", SqlDbType.NVarChar, -1) { Value = bundleJson };
        var p3 = new SqlParameter("@opt_args", SqlDbType.VarChar, 100) { Value = DBNull.Value };

        _logger.LogDebug("Executing {ProcName}", ProcName);

        using var timer = AppMetrics.DbProcDuration.WithLabels(ProcName).NewTimer();
        try
        {
            var results = await _ctx.Database
                .SqlQueryRaw<ConfiguratorPricingResult>(
                    "EXEC usp_cart_select_license_configurator_pricing @item_json, @bundle_json, @opt_args",
                    p1, p2, p3)
                .ToListAsync();

            AppMetrics.DbProcCalls.WithLabels(ProcName, "success").Inc();
            _logger.LogDebug("{ProcName} returned {RowCount} row(s)", ProcName, results.Count);
            return results;
        }
        catch
        {
            AppMetrics.DbProcCalls.WithLabels(ProcName, "error").Inc();
            throw;
        }
    }
}
