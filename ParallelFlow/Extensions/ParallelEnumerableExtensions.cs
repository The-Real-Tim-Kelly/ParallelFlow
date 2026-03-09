namespace ParallelFlow.Extensions;

/// <summary>
/// Provides extension methods for processing IEnumerable collections with asynchronous parallelism.
/// </summary>
public static class ParallelEnumerableExtensions
{
    /// <summary>
    /// Transforms each element of a collection asynchronously with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
    /// <typeparam name="TResult">The type of elements in the result collection.</typeparam>
    /// <param name="source">The source collection to transform.</param>
    /// <param name="selector">An async function to transform each element.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent operations.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of transformed elements in the original order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source or selector is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxDegreeOfParallelism is less than 1.</exception>
    public static async Task<List<TResult>> SelectParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, Task<TResult>> selector,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        if (maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Must be at least 1.");
        }

        var sourceList = source as IList<TSource> ?? source.ToList();
        var results = new TResult[sourceList.Count];

        await Parallel.ForEachAsync(
            sourceList.Select((item, index) => (item, index)),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (tuple, ct) =>
            {
                var (item, index) = tuple;
                results[index] = await selector(item).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return results.ToList();
    }

    /// <summary>
    /// Performs an asynchronous action on each element of a collection with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
    /// <param name="source">The source collection to process.</param>
    /// <param name="action">An async function to execute for each element.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent operations.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source or action is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxDegreeOfParallelism is less than 1.</exception>
    public static async Task ForEachParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, Task> action,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        if (maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Must be at least 1.");
        }

        await Parallel.ForEachAsync(
            source,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (item, ct) =>
            {
                await action(item).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Splits a collection into batches of a specified size.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The source collection to split into batches.</param>
    /// <param name="batchSize">The maximum number of elements per batch.</param>
    /// <returns>An enumerable of batches, where each batch is a list of elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1.</exception>
    public static IEnumerable<List<T>> Batch<T>(
        this IEnumerable<T> source,
        int batchSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Must be at least 1.");
        }

        var batch = new List<T>(batchSize);

        foreach (var item in source)
        {
            batch.Add(item);

            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
}
