using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Helpers;

/// <summary>
/// Extension methods for common EF Core query patterns with performance optimizations
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Execute multiple independent queries in parallel using Task.WhenAll
    /// </summary>
    public static async Task<(T1, T2)> ExecuteParallelAsync<T1, T2>(
        Task<T1> query1,
        Task<T2> query2)
    {
        var results = await Task.WhenAll(
            query1.ContinueWith(t => (object)t.Result!),
            query2.ContinueWith(t => (object)t.Result!)
        ).ConfigureAwait(false);

        return ((T1)results[0], (T2)results[1]);
    }

    /// <summary>
    /// Execute three independent queries in parallel
    /// </summary>
    public static async Task<(T1, T2, T3)> ExecuteParallelAsync<T1, T2, T3>(
        Task<T1> query1,
        Task<T2> query2,
        Task<T3> query3)
    {
        var results = await Task.WhenAll(
            query1.ContinueWith(t => (object)t.Result!),
            query2.ContinueWith(t => (object)t.Result!),
            query3.ContinueWith(t => (object)t.Result!)
        ).ConfigureAwait(false);

        return ((T1)results[0], (T2)results[1], (T3)results[2]);
    }

    /// <summary>
    /// Apply AsNoTracking and execute FirstOrDefaultAsync in one call
    /// Use for read-only single-entity queries
    /// </summary>
    public static Task<T?> FirstOrDefaultNoTrackingAsync<T>(
        this IQueryable<T> query,
        CancellationToken ct = default) where T : class
    {
        return query.AsNoTracking().FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Apply AsNoTracking and execute ToListAsync in one call
    /// Use for read-only collection queries
    /// </summary>
    public static Task<List<T>> ToListNoTrackingAsync<T>(
        this IQueryable<T> query,
        CancellationToken ct = default) where T : class
    {
        return query.AsNoTracking().ToListAsync(ct);
    }

    /// <summary>
    /// Apply AsNoTracking and execute SingleOrDefaultAsync in one call
    /// Use for read-only queries that should return exactly 0 or 1 result
    /// </summary>
    public static Task<T?> SingleOrDefaultNoTrackingAsync<T>(
        this IQueryable<T> query,
        CancellationToken ct = default) where T : class
    {
        return query.AsNoTracking().SingleOrDefaultAsync(ct);
    }

    /// <summary>
    /// Apply AsNoTracking and execute ToDictionaryAsync in one call
    /// Use for read-only dictionary/lookup queries
    /// </summary>
    public static Task<Dictionary<TKey, TValue>> ToDictionaryNoTrackingAsync<TSource, TKey, TValue>(
        this IQueryable<TSource> query,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        CancellationToken ct = default) where TSource : class where TKey : notnull
    {
        return query.AsNoTracking().ToDictionaryAsync(keySelector, valueSelector, ct);
    }
}
