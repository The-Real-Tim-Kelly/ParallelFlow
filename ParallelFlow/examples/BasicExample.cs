using ParallelFlow.Extensions;

namespace ParallelFlow.Examples;

/// <summary>
/// Demonstrates basic usage of ParallelFlow extension methods.
/// </summary>
public static class BasicExample
{
    /// <summary>
    /// Example: Transform a collection of URLs by downloading their content in parallel.
    /// </summary>
    public static async Task SelectParallelExample()
    {
        var urls = new[]
        {
            "https://api.example.com/data/1",
            "https://api.example.com/data/2",
            "https://api.example.com/data/3",
            "https://api.example.com/data/4",
            "https://api.example.com/data/5"
        };

        // Download all URLs with max 3 concurrent requests
        var results = await urls.SelectParallelAsync(
            async url => await DownloadContentAsync(url),
            maxDegreeOfParallelism: 3);

        Console.WriteLine($"Downloaded {results.Count} items");
    }

    /// <summary>
    /// Example: Process items without returning results.
    /// </summary>
    public static async Task ForEachParallelExample()
    {
        var userIds = Enumerable.Range(1, 100);

        // Send notifications to 100 users, 10 at a time
        await userIds.ForEachParallelAsync(
            async userId => await SendNotificationAsync(userId),
            maxDegreeOfParallelism: 10);

        Console.WriteLine("All notifications sent");
    }

    /// <summary>
    /// Example: Batch processing with parallelism.
    /// </summary>
    public static async Task BatchExample()
    {
        var items = Enumerable.Range(1, 1000);

        // Split 1000 items into batches of 100, process 5 batches concurrently
        await items
            .Batch(100)
            .ForEachParallelAsync(
                async batch => await ProcessBatchAsync(batch),
                maxDegreeOfParallelism: 5);

        Console.WriteLine("All batches processed");
    }

    /// <summary>
    /// Example: Combining Batch and SelectParallelAsync.
    /// </summary>
    public static async Task BatchWithSelectExample()
    {
        var items = Enumerable.Range(1, 500);

        // Process items in batches and collect results
        var batchResults = await items
            .Batch(50)
            .SelectParallelAsync(
                async batch => await ProcessBatchWithResultAsync(batch),
                maxDegreeOfParallelism: 4);

        Console.WriteLine($"Processed {batchResults.Count} batches");
    }

    /// <summary>
    /// Example: Using cancellation tokens.
    /// </summary>
    public static async Task CancellationExample()
    {
        var items = Enumerable.Range(1, 100);
        var cts = new CancellationTokenSource();

        // Cancel after 5 seconds
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await items.ForEachParallelAsync(
                async item => await ProcessItemAsync(item),
                maxDegreeOfParallelism: 5,
                cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was cancelled");
        }
    }

    // Helper methods (simulated async operations)
    private static async Task<string> DownloadContentAsync(string url)
    {
        await Task.Delay(100); // Simulate network delay
        return $"Content from {url}";
    }

    private static async Task SendNotificationAsync(int userId)
    {
        await Task.Delay(50); // Simulate sending notification
    }

    private static async Task ProcessBatchAsync(List<int> batch)
    {
        await Task.Delay(200); // Simulate batch processing
    }

    private static async Task<int> ProcessBatchWithResultAsync(List<int> batch)
    {
        await Task.Delay(200); // Simulate batch processing
        return batch.Sum();
    }

    private static async Task ProcessItemAsync(int item)
    {
        await Task.Delay(100); // Simulate processing
    }
}
