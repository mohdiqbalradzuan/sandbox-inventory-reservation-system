using InventoryReservation.Core.Models;
using InventoryReservation.Core.Services;
using Xunit;

namespace InventoryReservation.Tests.Concurrency;

public sealed class ReservationConcurrencyTests
{
    private static InMemoryInventoryService CreateService(int stock)
    {
        return new InMemoryInventoryService(new Product
        {
            Id = Guid.NewGuid(),
            Sku = "TEST-001",
            DisplayName = "Test Product",
            TotalStock = stock
        });
    }

    [Fact(Skip = "Enable after the Level 1 Reserve implementation is complete")]
    public async Task TenConcurrentRequestsWithTenStockShouldProduceTenSuccesses()
    {
        var service = CreateService(10);
        using var startGate = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(1, 10)
            .Select(i => Task.Run(() =>
            {
                startGate.Wait();
                return service.Reserve($"user-{i:000}", 1);
            }))
            .ToArray();

        startGate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(10, results.Count(x => x.Success));
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }

    [Fact(Skip = "Enable after the Level 1 Reserve implementation is complete")]
    public async Task TenConcurrentRequestsWithNineStockShouldProduceNineSuccesses()
    {
        var service = CreateService(9);
        using var startGate = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(1, 10)
            .Select(i => Task.Run(() =>
            {
                startGate.Wait();
                return service.Reserve($"user-{i:000}", 1);
            }))
            .ToArray();

        startGate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(9, results.Count(x => x.Success));
        Assert.Equal(1, results.Count(x => !x.Success));
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }

    // Level 3 target:
    // stock = 1, 500 simultaneous requests => 1 success, 499 failures.
}
