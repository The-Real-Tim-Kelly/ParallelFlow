# ParallelFlow

![Build Status](https://github.com/The-Real-Tim-Kelly/ParallelFlow/actions/workflows/build.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)

ParallelFlow is a lightweight .NET library that provides intuitive extension methods for processing collections asynchronously with controlled parallelism. If you've ever found yourself overwhelmed by managing thousands of concurrent API requests or wrestling with complex semaphore code, this library is designed to solve those exact challenges.

## What is ParallelFlow?

ParallelFlow extends the familiar LINQ-style syntax to asynchronous operations, giving you fine-grained control over concurrent processing. It's purpose-built for scenarios where you need to balance throughput with resource constraints—whether that's API rate limits, database connection pools, or system resources.

## The Problem

When processing collections asynchronously, you often face a dilemma: process items sequentially (slow) or all at once (overwhelming). Neither approach is ideal when:

- External APIs enforce rate limits
- Database connection pools have finite capacity
- System resources need to be managed efficiently
- Graceful degradation under load is a requirement

Without proper tooling, you typically end up with complex boilerplate code like this:

```csharp
var semaphore = new SemaphoreSlim(10);
var tasks = items.Select(async item => 
{
    await semaphore.WaitAsync();
    try 
    { 
        return await ProcessAsync(item); 
    }
    finally 
    { 
        semaphore.Release(); 
    }
});
await Task.WhenAll(tasks);
```

Alternatively, you might use `Task.WhenAll()` without throttling, which can quickly exhaust resources under high load.

## The Solution

```csharp
var results = await items.SelectParallelAsync(
    async item => await ProcessAsync(item),
    maxDegreeOfParallelism: 10);
```

Clean, readable, and maintainable—exactly what production code should look like.

## Installation

Currently available via source. Clone this repository or copy the source files into your project. A NuGet package release is planned for the near future.

**Requirements:**
- .NET 10 or higher
- No external dependencies

## API Overview

### SelectParallelAsync

Transform each element of a collection asynchronously while maintaining the original order of results.

```csharp
var urls = new[] 
{ 
    "https://api.example.com/users/1",
    "https://api.example.com/users/2",
    "https://api.example.com/users/3"
};

// Process 5 URLs concurrently instead of all at once
var users = await urls.SelectParallelAsync(
    async url => await httpClient.GetAsync<User>(url),
    maxDegreeOfParallelism: 5);
```

Results are returned in the same order as the input collection, ensuring predictable behavior when order matters.

### ForEachParallelAsync

Execute an asynchronous action on each element without collecting results. Ideal for side-effect operations like sending notifications, logging, or updating external systems.

```csharp
// Send 10 emails concurrently instead of 1000
await users.ForEachParallelAsync(
    async user => await emailService.SendWelcomeEmail(user),
    maxDegreeOfParallelism: 10);
```

### Batch

Partition large collections into manageable chunks. Particularly useful for bulk database operations and batch processing scenarios.

```csharp
var items = GetThousandsOfRecords();

// Process 100 records at a time
foreach (var batch in items.Batch(100))
{
    await SaveToDatabase(batch);
}
```

## Usage Examples

### Working with Rate-Limited APIs

```csharp
// Respect API rate limits by controlling concurrent requests
var results = await userIds
    .SelectParallelAsync(
        async id => await api.GetUser(id),
        maxDegreeOfParallelism: 10);
```

### Bulk Database Operations

```csharp
// Batch records and process multiple batches concurrently
await records
    .Batch(1000)
    .ForEachParallelAsync(
        async batch => await db.BulkInsert(batch),
        maxDegreeOfParallelism: 5);
```

### File Processing

```csharp
var files = Directory.GetFiles("./data");

var processed = await files.SelectParallelAsync(
    async file => await ProcessFile(file),
    maxDegreeOfParallelism: Environment.ProcessorCount);
```

### Cancellation Support

```csharp
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    await items.ForEachParallelAsync(
        async item => await Process(item),
        maxDegreeOfParallelism: 10,
        cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    // Handle cancellation gracefully
}
```

## Choosing the Right Degree of Parallelism

Based on practical experience, here are recommended starting points:

- **API calls**: Start with 10-20. Adjust based on rate limit responses and performance monitoring.
- **Database queries**: Align with your connection pool size (typically 100 by default).
- **CPU-bound operations**: Use `Environment.ProcessorCount`, or up to 2x that value for operations with I/O components.
- **File operations**: 4-8 concurrent operations work well for most SSDs; adjust based on your storage characteristics.

**General guidance**: Start conservative and increase incrementally while monitoring performance. The optimal value depends on your specific workload and infrastructure.

## Implementation Details

ParallelFlow is built on top of .NET's `Parallel.ForEachAsync`, providing a more intuitive API without reinventing core concurrency primitives.

**Key characteristics:**
- **Thread-safe**: All operations are safe for concurrent use
- **Order-preserving**: `SelectParallelAsync` maintains input order in results
- **Exception handling**: Properly propagates exceptions and cancels remaining work
- **Minimal footprint**: Approximately 130 lines of focused, production-ready code

## Testing

The library includes comprehensive test coverage with 58 unit and integration tests covering edge cases, error conditions, and concurrent behavior.

Run the test suite:

```bash
dotnet test
```

All tests pass consistently across different environments and configurations.

## Contributing

Contributions are welcome! This library intentionally maintains a focused scope—please keep additions aligned with the core mission of simplified parallel async operations.

**Guidelines:**
- Ensure new features solve real-world problems
- Include comprehensive tests for all changes
- Maintain the existing code style and documentation quality
- Keep the API surface small and intuitive

## License

MIT License. Free for commercial and personal use. See LICENSE file for full details.

## Motivation

This library was born from repeatedly implementing the same parallel async patterns across multiple projects. Rather than continuing to write boilerplate semaphore code, I extracted the common patterns into a reusable, well-tested library.

The goal is simple: save developers time and reduce the likelihood of concurrency bugs in production code. If this library helps you ship more reliable async code faster, it's achieved its purpose.
