using InventoryReservation.Core.Models;
using InventoryReservation.Core.Services;
using Xunit;

namespace InventoryReservation.Tests.Unit;

public sealed class BasicReservationTests
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

    [Fact(Skip = "TDD starting point: implement Reserve first")]
    public void ReserveOneItemWhenStockAvailableShouldSucceed()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.True(result.Success);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }

    [Fact(Skip = "TDD starting point: implement Reserve first")]
    public void ReserveWhenStockUnavailableShouldFail()
    {
        var service = CreateService(0);

        var result = service.Reserve("user-001", 1);

        Assert.False(result.Success);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }
}
