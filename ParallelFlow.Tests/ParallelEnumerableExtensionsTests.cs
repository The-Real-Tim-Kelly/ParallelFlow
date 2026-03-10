using ParallelFlow.Extensions;
using System.Collections.Concurrent;

namespace ParallelFlow.Tests;

[TestClass]
public sealed class ParallelEnumerableExtensionsTests
{
    [TestMethod]
    public async Task SelectParallelAsync_ReturnsCorrectResults()
    {
        var source = Enumerable.Range(1, 100);

        var results = await source.SelectParallelAsync(
            async x =>
            {
                await Task.Delay(1);
                return x * 2;
            },
            maxDegreeOfParallelism: 10);

        Assert.AreEqual(100, results.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual((i + 1) * 2, results[i]);
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_PreservesOrder()
    {
        var source = Enumerable.Range(1, 50);

        var results = await source.SelectParallelAsync(
            async x =>
            {
                await Task.Delay(Random.Shared.Next(1, 10));
                return x;
            },
            maxDegreeOfParallelism: 10);

        CollectionAssert.AreEqual(source.ToList(), results);
    }

    [TestMethod]
    public async Task SelectParallelAsync_RespectsMaxDegreeOfParallelism()
    {
        var source = Enumerable.Range(1, 20);
        var concurrentCount = 0;
        var maxObservedConcurrency = 0;
        var lockObject = new object();

        await source.SelectParallelAsync(
            async x =>
            {
                lock (lockObject)
                {
                    concurrentCount++;
                    if (concurrentCount > maxObservedConcurrency)
                    {
                        maxObservedConcurrency = concurrentCount;
                    }
                }

                await Task.Delay(50);

                lock (lockObject)
                {
                    concurrentCount--;
                }

                return x;
            },
            maxDegreeOfParallelism: 5);

        Assert.IsTrue(maxObservedConcurrency <= 5);
        Assert.IsTrue(maxObservedConcurrency > 1, "Should have used parallelism");
    }

    [TestMethod]
    public async Task SelectParallelAsync_ThrowsOnNullSource()
    {
        IEnumerable<int>? source = null;

        try
        {
            await source!.SelectParallelAsync(async x => x, 5);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_ThrowsOnNullSelector()
    {
        var source = Enumerable.Range(1, 10);

        try
        {
            await source.SelectParallelAsync<int, int>(null!, 5);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_ThrowsOnInvalidMaxDegree()
    {
        var source = Enumerable.Range(1, 10);

        try
        {
            await source.SelectParallelAsync(async x => x, 0);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_HandlesCancellation()
    {
        var source = Enumerable.Range(1, 100);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        try
        {
            await source.SelectParallelAsync(
                async x =>
                {
                    await Task.Delay(100);
                    return x;
                },
                maxDegreeOfParallelism: 5,
                cancellationToken: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_WorksWithEmptyCollection()
    {
        var source = Enumerable.Empty<int>();

        var results = await source.SelectParallelAsync(
            async x => x * 2,
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task ForEachParallelAsync_ExecutesAllItems()
    {
        var source = Enumerable.Range(1, 50);
        var processedItems = new ConcurrentBag<int>();

        await source.ForEachParallelAsync(
            async x =>
            {
                await Task.Delay(1);
                processedItems.Add(x);
            },
            maxDegreeOfParallelism: 10);

        Assert.AreEqual(50, processedItems.Count);
        CollectionAssert.AreEquivalent(source.ToList(), processedItems.ToList());
    }

    [TestMethod]
    public async Task ForEachParallelAsync_RespectsMaxDegreeOfParallelism()
    {
        var source = Enumerable.Range(1, 20);
        var concurrentCount = 0;
        var maxObservedConcurrency = 0;
        var lockObject = new object();

        await source.ForEachParallelAsync(
            async x =>
            {
                lock (lockObject)
                {
                    concurrentCount++;
                    if (concurrentCount > maxObservedConcurrency)
                    {
                        maxObservedConcurrency = concurrentCount;
                    }
                }

                await Task.Delay(50);

                lock (lockObject)
                {
                    concurrentCount--;
                }
            },
            maxDegreeOfParallelism: 5);

        Assert.IsTrue(maxObservedConcurrency <= 5);
        Assert.IsTrue(maxObservedConcurrency > 1, "Should have used parallelism");
    }

    [TestMethod]
    public async Task ForEachParallelAsync_ThrowsOnNullSource()
    {
        IEnumerable<int>? source = null;

        try
        {
            await source!.ForEachParallelAsync(async x => { }, 5);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task ForEachParallelAsync_ThrowsOnNullAction()
    {
        var source = Enumerable.Range(1, 10);

        try
        {
            await source.ForEachParallelAsync(null!, 5);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task ForEachParallelAsync_ThrowsOnInvalidMaxDegree()
    {
        var source = Enumerable.Range(1, 10);

        try
        {
            await source.ForEachParallelAsync(async x => { }, -1);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task ForEachParallelAsync_HandlesCancellation()
    {
        var source = Enumerable.Range(1, 100);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        try
        {
            await source.ForEachParallelAsync(
                async x => await Task.Delay(100),
                maxDegreeOfParallelism: 5,
                cancellationToken: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void Batch_SplitsCollectionCorrectly()
    {
        var source = Enumerable.Range(1, 1000);

        var batches = source.Batch(100).ToList();

        Assert.AreEqual(10, batches.Count);
        Assert.AreEqual(100, batches[0].Count);
        Assert.AreEqual(100, batches[9].Count);
    }

    [TestMethod]
    public void Batch_HandlesUnevenBatches()
    {
        var source = Enumerable.Range(1, 105);

        var batches = source.Batch(100).ToList();

        Assert.AreEqual(2, batches.Count);
        Assert.AreEqual(100, batches[0].Count);
        Assert.AreEqual(5, batches[1].Count);
    }

    [TestMethod]
    public void Batch_PreservesOrder()
    {
        var source = Enumerable.Range(1, 250);

        var batches = source.Batch(100).ToList();

        Assert.AreEqual(1, batches[0][0]);
        Assert.AreEqual(100, batches[0][99]);
        Assert.AreEqual(101, batches[1][0]);
        Assert.AreEqual(200, batches[1][99]);
        Assert.AreEqual(201, batches[2][0]);
    }

    [TestMethod]
    public void Batch_HandlesEmptyCollection()
    {
        var source = Enumerable.Empty<int>();

        var batches = source.Batch(10).ToList();

        Assert.AreEqual(0, batches.Count);
    }

    [TestMethod]
    public void Batch_HandlesSingleItemBatches()
    {
        var source = Enumerable.Range(1, 5);

        var batches = source.Batch(1).ToList();

        Assert.AreEqual(5, batches.Count);
        Assert.AreEqual(1, batches[0][0]);
        Assert.AreEqual(5, batches[4][0]);
    }

    [TestMethod]
    public void Batch_ThrowsOnNullSource()
    {
        IEnumerable<int>? source = null;

        try
        {
            source!.Batch(10).ToList();
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void Batch_ThrowsOnInvalidBatchSize()
    {
        var source = Enumerable.Range(1, 10);

        try
        {
            source.Batch(0).ToList();
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithBatch_IntegrationTest()
    {
        var source = Enumerable.Range(1, 500);

        var batchResults = await source
            .Batch(50)
            .SelectParallelAsync(
                async batch =>
                {
                    await Task.Delay(10);
                    return batch.Sum();
                },
                maxDegreeOfParallelism: 5);

        Assert.AreEqual(10, batchResults.Count);

        var expectedSum = Enumerable.Range(1, 500).Sum();
        var actualSum = batchResults.Sum();
        Assert.AreEqual(expectedSum, actualSum);
    }

    [TestMethod]
    public async Task SelectParallelAsync_PropagatesExceptions()
    {
        var source = Enumerable.Range(1, 10);

        try
        {
            await source.SelectParallelAsync(
                async x =>
                {
                    await Task.Delay(1);
                    if (x == 5) throw new InvalidOperationException("Test exception");
                    return x;
                },
                maxDegreeOfParallelism: 5);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task ForEachParallelAsync_PropagatesExceptions()
    {
        var source = Enumerable.Range(1, 10);

        try
        {
            await source.ForEachParallelAsync(
                async x =>
                {
                    await Task.Delay(1);
                    if (x == 5) throw new InvalidOperationException("Test exception");
                },
                maxDegreeOfParallelism: 5);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    // Additional Coverage Tests

    [TestMethod]
    public async Task SelectParallelAsync_WorksWithIList()
    {
        // Test the optimization path where source is already an IList
        var source = new List<int> { 1, 2, 3, 4, 5 };

        var results = await source.SelectParallelAsync(
            async x =>
            {
                await Task.Delay(1);
                return x * 2;
            },
            maxDegreeOfParallelism: 3);

        Assert.AreEqual(5, results.Count);
        CollectionAssert.AreEqual(new[] { 2, 4, 6, 8, 10 }, results);
    }

    [TestMethod]
    public async Task SelectParallelAsync_WorksWithArray()
    {
        // Test the optimization path where source is already an IList (arrays implement IList)
        var source = new[] { 10, 20, 30 };

        var results = await source.SelectParallelAsync(
            async x =>
            {
                await Task.Delay(1);
                return x.ToString();
            },
            maxDegreeOfParallelism: 2);

        CollectionAssert.AreEqual(new[] { "10", "20", "30" }, results);
    }

    [TestMethod]
    public async Task SelectParallelAsync_SequentialProcessing()
    {
        // Test with maxDegreeOfParallelism = 1 (sequential processing)
        var source = Enumerable.Range(1, 10);
        var processOrder = new ConcurrentBag<int>();

        await source.SelectParallelAsync(
            async x =>
            {
                processOrder.Add(x);
                await Task.Delay(5);
                return x;
            },
            maxDegreeOfParallelism: 1);

        Assert.AreEqual(10, processOrder.Count);
    }

    [TestMethod]
    public async Task SelectParallelAsync_SingleItemCollection()
    {
        var source = new[] { 42 };

        var results = await source.SelectParallelAsync(
            async x => await Task.FromResult(x * 2),
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(84, results[0]);
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithNullableResults()
    {
        var source = Enumerable.Range(1, 5);

        var results = await source.SelectParallelAsync(
            async x =>
            {
                await Task.Delay(1);
                return x % 2 == 0 ? (int?)x : null;
            },
            maxDegreeOfParallelism: 3);

        Assert.AreEqual(5, results.Count);
        Assert.IsNull(results[0]);
        Assert.AreEqual(2, results[1]);
        Assert.IsNull(results[2]);
        Assert.AreEqual(4, results[3]);
        Assert.IsNull(results[4]);
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithComplexTypes()
    {
        var source = new[] { "apple", "banana", "cherry" };

        var results = await source.SelectParallelAsync(
            async item => await Task.FromResult(new { Name = item, Length = item.Length }),
            maxDegreeOfParallelism: 2);

        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("apple", results[0].Name);
        Assert.AreEqual(5, results[0].Length);
        Assert.AreEqual("banana", results[1].Name);
        Assert.AreEqual(6, results[1].Length);
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithHighParallelism()
    {
        var source = Enumerable.Range(1, 100);

        var results = await source.SelectParallelAsync(
            async x => await Task.FromResult(x * 2),
            maxDegreeOfParallelism: 100);

        Assert.AreEqual(100, results.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual((i + 1) * 2, results[i]);
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithSynchronousSelector()
    {
        var source = Enumerable.Range(1, 10);

        var results = await source.SelectParallelAsync(
            x => Task.FromResult(x * 3),
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(10, results.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual((i + 1) * 3, results[i]);
        }
    }

    [TestMethod]
    public async Task ForEachParallelAsync_SequentialProcessing()
    {
        // Test with maxDegreeOfParallelism = 1 (sequential processing)
        var source = Enumerable.Range(1, 10);
        var processedItems = new ConcurrentBag<int>();

        await source.ForEachParallelAsync(
            async x =>
            {
                processedItems.Add(x);
                await Task.Delay(1);
            },
            maxDegreeOfParallelism: 1);

        Assert.AreEqual(10, processedItems.Count);
    }

    [TestMethod]
    public async Task ForEachParallelAsync_SingleItemCollection()
    {
        var source = new[] { 1 };
        var executed = false;

        await source.ForEachParallelAsync(
            async x =>
            {
                executed = true;
                await Task.Delay(1);
            },
            maxDegreeOfParallelism: 5);

        Assert.IsTrue(executed);
    }

    [TestMethod]
    public async Task ForEachParallelAsync_WithEmptyCollection()
    {
        var source = Enumerable.Empty<int>();
        var count = 0;

        await source.ForEachParallelAsync(
            async x =>
            {
                count++;
                await Task.Delay(1);
            },
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task ForEachParallelAsync_WithHighParallelism()
    {
        var source = Enumerable.Range(1, 100);
        var processedItems = new ConcurrentBag<int>();

        await source.ForEachParallelAsync(
            async x =>
            {
                processedItems.Add(x);
                await Task.Delay(1);
            },
            maxDegreeOfParallelism: 50);

        Assert.AreEqual(100, processedItems.Count);
    }

    [TestMethod]
    public async Task ForEachParallelAsync_WithSynchronousAction()
    {
        var source = Enumerable.Range(1, 10);
        var processedItems = new ConcurrentBag<int>();

        await source.ForEachParallelAsync(
            x =>
            {
                processedItems.Add(x);
                return Task.CompletedTask;
            },
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(10, processedItems.Count);
    }

    [TestMethod]
    public void Batch_WithBatchSizeLargerThanCollection()
    {
        var source = Enumerable.Range(1, 5);

        var batches = source.Batch(10).ToList();

        Assert.AreEqual(1, batches.Count);
        Assert.AreEqual(5, batches[0].Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, batches[0]);
    }

    [TestMethod]
    public void Batch_WithBatchSizeEqualToCollection()
    {
        var source = Enumerable.Range(1, 10);

        var batches = source.Batch(10).ToList();

        Assert.AreEqual(1, batches.Count);
        Assert.AreEqual(10, batches[0].Count);
    }

    [TestMethod]
    public void Batch_CanBeEnumeratedMultipleTimes()
    {
        var source = Enumerable.Range(1, 10);
        var batchedSource = source.Batch(3);

        var firstEnumeration = batchedSource.ToList();
        var secondEnumeration = batchedSource.ToList();

        Assert.AreEqual(4, firstEnumeration.Count);
        Assert.AreEqual(4, secondEnumeration.Count);

        for (int i = 0; i < firstEnumeration.Count; i++)
        {
            CollectionAssert.AreEqual(firstEnumeration[i], secondEnumeration[i]);
        }
    }

    [TestMethod]
    public void Batch_WithSingleItem()
    {
        var source = new[] { 42 };

        var batches = source.Batch(5).ToList();

        Assert.AreEqual(1, batches.Count);
        Assert.AreEqual(1, batches[0].Count);
        Assert.AreEqual(42, batches[0][0]);
    }

    [TestMethod]
    public void Batch_WithComplexTypes()
    {
        var source = new[]
        {
            new { Id = 1, Name = "A" },
            new { Id = 2, Name = "B" },
            new { Id = 3, Name = "C" }
        };

        var batches = source.Batch(2).ToList();

        Assert.AreEqual(2, batches.Count);
        Assert.AreEqual(2, batches[0].Count);
        Assert.AreEqual(1, batches[1].Count);
        Assert.AreEqual("C", batches[1][0].Name);
    }

    [TestMethod]
    public void Batch_LazyEvaluation()
    {
        var evaluationCount = 0;
        var source = Enumerable.Range(1, 10).Select(x =>
        {
            evaluationCount++;
            return x;
        });

        var batches = source.Batch(3);

        // Should not evaluate yet
        Assert.AreEqual(0, evaluationCount);

        // Now evaluate
        var result = batches.ToList();

        Assert.AreEqual(10, evaluationCount);
        Assert.AreEqual(4, result.Count);
    }

    [TestMethod]
    public async Task ForEachParallelAsync_WithBatch_IntegrationTest()
    {
        var source = Enumerable.Range(1, 100);
        var processedBatches = new ConcurrentBag<int>();

        await source
            .Batch(10)
            .ForEachParallelAsync(
                async batch =>
                {
                    processedBatches.Add(batch.Count);
                    await Task.Delay(10);
                },
                maxDegreeOfParallelism: 3);

        Assert.AreEqual(10, processedBatches.Count);
        Assert.IsTrue(processedBatches.All(count => count == 10));
    }

    [TestMethod]
    public async Task SelectParallelAsync_ChainedTransformations()
    {
        var source = Enumerable.Range(1, 20);

        var results = await source
            .SelectParallelAsync(
                async x => await Task.FromResult(x * 2),
                maxDegreeOfParallelism: 5)
            .ContinueWith(t => t.Result.SelectParallelAsync(
                async x => await Task.FromResult(x + 10),
                maxDegreeOfParallelism: 5))
            .Unwrap();

        Assert.AreEqual(20, results.Count);
        Assert.AreEqual(12, results[0]); // (1 * 2) + 10
        Assert.AreEqual(50, results[19]); // (20 * 2) + 10
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithPreCancelledToken()
    {
        var source = Enumerable.Range(1, 10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await source.SelectParallelAsync(
                async x => await Task.FromResult(x),
                maxDegreeOfParallelism: 5,
                cancellationToken: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task ForEachParallelAsync_WithPreCancelledToken()
    {
        var source = Enumerable.Range(1, 10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await source.ForEachParallelAsync(
                async x => await Task.Delay(1),
                maxDegreeOfParallelism: 5,
                cancellationToken: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithLargeCollection()
    {
        var source = Enumerable.Range(1, 10000);

        var results = await source.SelectParallelAsync(
            async x => await Task.FromResult(x % 2 == 0),
            maxDegreeOfParallelism: 20);

        Assert.AreEqual(10000, results.Count);
        Assert.AreEqual(5000, results.Count(x => x));
    }

    [TestMethod]
    public async Task ForEachParallelAsync_WithLargeCollection()
    {
        var source = Enumerable.Range(1, 10000);
        var count = 0;
        var lockObj = new object();

        await source.ForEachParallelAsync(
            async x =>
            {
                await Task.Delay(0);
                lock (lockObj)
                {
                    count++;
                }
            },
            maxDegreeOfParallelism: 20);

        Assert.AreEqual(10000, count);
    }

    [TestMethod]
    public void Batch_WithVeryLargeBatchSize()
    {
        var source = Enumerable.Range(1, 10);

        var batches = source.Batch(1000000).ToList();

        Assert.AreEqual(1, batches.Count);
        Assert.AreEqual(10, batches[0].Count);
    }

    [TestMethod]
    public async Task SelectParallelAsync_MaintainsOrderWithDifferentProcessingTimes()
    {
        var source = Enumerable.Range(1, 20);

        var results = await source.SelectParallelAsync(
            async x =>
            {
                // Make later items finish faster
                await Task.Delay(21 - x);
                return x;
            },
            maxDegreeOfParallelism: 10);

        // Should still maintain input order
        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(i + 1, results[i]);
        }
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithDefaultCancellationToken()
    {
        var source = Enumerable.Range(1, 10);

        var results = await source.SelectParallelAsync(
            async x => await Task.FromResult(x * 2),
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(10, results.Count);
    }

    [TestMethod]
    public async Task ForEachParallelAsync_WithDefaultCancellationToken()
    {
        var source = Enumerable.Range(1, 10);
        var count = 0;

        await source.ForEachParallelAsync(
            async x =>
            {
                Interlocked.Increment(ref count);
                await Task.Delay(1);
            },
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(10, count);
    }

    [TestMethod]
    public void Batch_PreservesOrderWithMultipleEnumerations()
    {
        var source = new[] { 5, 4, 3, 2, 1 };
        var batched = source.Batch(2);

        var first = batched.ToList();
        var second = batched.ToList();

        Assert.AreEqual(5, first[0][0]);
        Assert.AreEqual(5, second[0][0]);
        Assert.AreEqual(1, first[2][0]);
        Assert.AreEqual(1, second[2][0]);
    }

    [TestMethod]
    public async Task IntegrationTest_ComplexPipeline()
    {
        // Create a complex pipeline using all three methods
        var source = Enumerable.Range(1, 100);

        // Batch into groups of 10, process each batch in parallel, collect results
        var batchSums = await source
            .Batch(10)
            .SelectParallelAsync(
                async batch =>
                {
                    await Task.Delay(5);
                    return batch.Sum();
                },
                maxDegreeOfParallelism: 3);

        // Then process those results again
        var processedResults = new ConcurrentBag<int>();
        await batchSums.ForEachParallelAsync(
            async sum =>
            {
                await Task.Delay(1);
                processedResults.Add(sum);
            },
            maxDegreeOfParallelism: 5);

        Assert.AreEqual(10, batchSums.Count);
        Assert.AreEqual(10, processedResults.Count);

        // Verify the total sum is correct
        var expectedTotal = Enumerable.Range(1, 100).Sum();
        var actualTotal = batchSums.Sum();
        Assert.AreEqual(expectedTotal, actualTotal);
    }

    [TestMethod]
    public async Task SelectParallelAsync_WithDuplicateValues()
    {
        var source = new[] { 1, 2, 2, 3, 3, 3, 4, 4, 4, 4 };

        var results = await source.SelectParallelAsync(
            async x => await Task.FromResult(x * 10),
            maxDegreeOfParallelism: 3);

        Assert.AreEqual(10, results.Count);
        CollectionAssert.AreEqual(new[] { 10, 20, 20, 30, 30, 30, 40, 40, 40, 40 }, results);
    }

    [TestMethod]
    public void Batch_WithNegativeNumbers()
    {
        var source = Enumerable.Range(-10, 20);

        var batches = source.Batch(5).ToList();

        Assert.AreEqual(4, batches.Count);
        Assert.AreEqual(-10, batches[0][0]);
        Assert.AreEqual(9, batches[3][4]);
    }

    [TestMethod]
    public async Task SelectParallelAsync_PreservesTypeInformation()
    {
        var source = new[] { 1, 2, 3 };

        var results = await source.SelectParallelAsync(
            async x => await Task.FromResult(x.ToString()),
            maxDegreeOfParallelism: 2);

        Assert.IsInstanceOfType(results, typeof(List<string>));
        Assert.AreEqual("1", results[0]);
    }
}
