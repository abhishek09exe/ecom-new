using Prometheus;

namespace ecom_new_api.Observability;

/// <summary>
/// Central registry for custom Prometheus metrics. Keeping every metric
/// definition here (rather than scattered CreateCounter/CreateHistogram calls)
/// avoids duplicate-registration errors and gives one place to see everything
/// this app exposes beyond the generic HTTP metrics from UseHttpMetrics().
///
/// Label values must stay low-cardinality and static (proc names, fixed
/// outcome strings) — never dynamic data like license_id/keycode/user IDs,
/// since Prometheus stores a new time series per unique label combination.
/// </summary>
public static class AppMetrics
{
    /// <summary>Every stored-proc / raw-SQL call, labeled by proc name and outcome (success|error).</summary>
    public static readonly Counter DbProcCalls = Metrics.CreateCounter(
        "ecom_db_proc_calls_total",
        "Total stored procedure / raw SQL calls",
        new CounterConfiguration { LabelNames = new[] { "procedure", "outcome" } });

    /// <summary>Duration of each stored-proc / raw-SQL call, labeled by proc name.</summary>
    public static readonly Histogram DbProcDuration = Metrics.CreateHistogram(
        "ecom_db_proc_duration_seconds",
        "Duration of stored procedure / raw SQL calls",
        new HistogramConfiguration
        {
            LabelNames = new[] { "procedure" },
            Buckets = Histogram.ExponentialBuckets(0.005, 2, 12) // 5ms → ~20s
        });

    /// <summary>Business-level operation outcomes (e.g. license lookup found/not_found/error).</summary>
    public static readonly Counter BusinessOperations = Metrics.CreateCounter(
        "ecom_business_operations_total",
        "Business-level operation outcomes",
        new CounterConfiguration { LabelNames = new[] { "operation", "outcome" } });

    /// <summary>Unhandled/logged exceptions by category (typically the exception type name).</summary>
    public static readonly Counter ErrorsTotal = Metrics.CreateCounter(
        "ecom_errors_total",
        "Application errors by category",
        new CounterConfiguration { LabelNames = new[] { "category" } });
}
