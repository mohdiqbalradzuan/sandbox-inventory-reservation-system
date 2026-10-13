using InventoryReservation.Core.Models;
using InventoryReservation.Core.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryReservation.Tests.Level1.BasicReservation;

public sealed class BasicReservationTests
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
    public void ReserveOneItemWhenStockAvailableShouldSucceed()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.True(result.Success);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }

    [Fact]
    public void ReserveWhenStockUnavailableShouldFail()
    {
        var service = CreateService(0);

        var result = service.Reserve("user-001", 1);

        Assert.False(result.Success);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }
}
