using InventoryReservation.Core.Models;
using InventoryReservation.Core.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryReservation.Tests.Level3.ConcurrentReservation;

public sealed class ReservationConcurrencyTests
{
    private static InMemoryInventoryService CreateService(
        int totalStock,
        TimeProvider? timeProvider = null,
        TimeSpan? holdDuration = null
    )
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = "TEST-001",
            DisplayName = "Test Product",
            TotalStock = totalStock
        };

        var options = Options.Create(new ReservationOptions
        {
            HoldDuration =
                holdDuration ?? TimeSpan.FromMinutes(2)
        });

        return new InMemoryInventoryService(
            product,
            timeProvider ?? TimeProvider.System,
            options);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task FiveHundredConcurrentRequestsWithOneStockShouldProduceOneSuccesses()
    {
        var service = CreateService(1);
        using var startGate = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(1, 500)
            .Select(i => Task.Run(() =>
            {
                startGate.Wait();
                return service.Reserve($"user-{i:000}", 1);
            }))
            .ToArray();

        startGate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(x => x.Success));
        Assert.Equal(499, results.Count(x => !x.Success));
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }
}
