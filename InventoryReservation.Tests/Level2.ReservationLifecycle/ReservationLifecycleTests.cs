
using InventoryReservation.Core.Models;
using InventoryReservation.Core.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace InventoryReservation.Tests.Level2.ReservationLifecycle;

public sealed class ReservationLifecycleTests
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
    public void ReserveWhenStockAvailableShouldCreateActiveReservation()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);
    }

    [Fact]
    public void ConfirmWhenReservationIsActiveShouldSetStatusToConfirmed()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.True(reservationToConfirm.Success);
        Assert.Equal(ReservationStatus.Confirmed, reservationToConfirm.Reservation?.Status);
    }

    [Fact]
    public void CancelWhenReservationIsActiveShouldSetStatusToCancelled()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        var reservationToCancel = service.Cancel(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToCancel);
        Assert.True(reservationToCancel.Success);
        Assert.Equal(ReservationStatus.Cancelled, reservationToCancel.Reservation?.Status);
    }

    [Fact]
    public void ExpireWhenReservationIsActiveAndHoldTimeExceededShouldSetStatusToExpired()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(121));
        var expiredReservationsCount = service.ExpireDueReservations();

        Assert.NotEqual(0, expiredReservationsCount);

        var expiredReservation = service.GetSnapshot().Reservations
            .FirstOrDefault(r => r.Id == result.Reservation?.Id);

        Assert.NotNull(expiredReservation);
        Assert.Equal(ReservationStatus.Expired, expiredReservation.Status);
    }

    [Fact]
    public void ReserveWhenOneStockAvailableShouldCreateOneActiveReservationAndResultZeroAvailableStock()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }

    [Fact]
    public void ConfirmWhenReservationIsActiveWithOneStockShouldSetStatusToConfirmedAndResultZeroAvailableStock()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);

        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.True(reservationToConfirm.Success);
        Assert.Equal(ReservationStatus.Confirmed, reservationToConfirm.Reservation?.Status);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }

    [Fact]
    public void CancelWhenReservationIsActiveWithOneStockShouldSetStatusToCancelledAndResultOneAvailableStock()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);

        var reservationToCancel = service.Cancel(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToCancel);
        Assert.True(reservationToCancel.Success);
        Assert.Equal(ReservationStatus.Cancelled, reservationToCancel.Reservation?.Status);
        Assert.Equal(1, service.GetSnapshot().AvailableStock);
    }

    [Fact]
    public void ExpireWhenReservationExpiresShouldReleaseReservedStock()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);
        Assert.Equal(0, service.GetSnapshot().AvailableStock);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(121));
        var expiredReservationsCount = service.ExpireDueReservations();

        Assert.NotEqual(0, expiredReservationsCount);
        Assert.Equal(1, service.GetSnapshot().AvailableStock);
    }

    [Fact]
    public void CancelWhenReservationIsConfirmedShouldNotReleaseStock()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Confirm the reservation first
        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.True(reservationToConfirm.Success);
        Assert.Equal(ReservationStatus.Confirmed, reservationToConfirm.Reservation?.Status);

        // Then try to cancel it
        service.Cancel(result.Reservation?.Id ?? Guid.Empty);

        Assert.Equal(0, service.GetSnapshot().AvailableStock);
    }

    [Fact]
    public void ConfirmWhenReservationIsCancelledShouldFail()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Cancel the reservation first
        service.Cancel(result.Reservation?.Id ?? Guid.Empty);

        // Then try to confirm it (should fail)
        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.False(reservationToConfirm.Success);
        Assert.Equal("INVALID_CONFIRM_RESERVATION_LIFECYCLE", reservationToConfirm.ErrorCode);
    }

    [Fact]
    public void ConfirmWhenReservationIsExpiredShouldFail()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(121));
        service.ExpireDueReservations();

        // Then try to confirm it (should fail)
        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.False(reservationToConfirm.Success);
        Assert.Equal("INVALID_CONFIRM_RESERVATION_LIFECYCLE", reservationToConfirm.ErrorCode);
    }

    [Fact]
    public void CancelWhenReservationIsConfirmedShouldFail()
    {
        var service = CreateService(1);

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Confirm the reservation first
        service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        // Then try to cancel it (should fail)
        var reservationToCancel = service.Cancel(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToCancel);
        Assert.False(reservationToCancel.Success);
        Assert.Equal("INVALID_CANCEL_RESERVATION_LIFECYCLE", reservationToCancel.ErrorCode);
    }

    [Fact]
    public void CancelWhenReservationIsExpiredShouldFail()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(121));
        service.ExpireDueReservations();

        // Then try to cancel it (should fail)
        var reservationToCancel = service.Cancel(result.Reservation?.Id ?? Guid.Empty);

        Assert.False(reservationToCancel.Success);
        Assert.Equal("INVALID_CANCEL_RESERVATION_LIFECYCLE", reservationToCancel.ErrorCode);
    }

    [Fact]
    public void ExpireWhenReservationIsConfirmedShouldNotChangeReservation()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Confirm the reservation first
        service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(121));
        service.ExpireDueReservations();

        var inventorySnapshot = service.GetSnapshot();

        Assert.Equal(1, inventorySnapshot.Reservations.Count(r => r.Id == result.Reservation?.Id && r.Status == ReservationStatus.Confirmed));
    }

    [Fact]
    public void ExpireWhenReservationIsCancelledShouldNotChangeReservation()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Cancel the reservation first
        service.Cancel(result.Reservation?.Id ?? Guid.Empty);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(121));
        service.ExpireDueReservations();

        var inventorySnapshot = service.GetSnapshot();

        Assert.Equal(1, inventorySnapshot.Reservations.Count(r => r.Id == result.Reservation?.Id && r.Status == ReservationStatus.Cancelled));
    }

    [Fact]
    public void ConfirmWhenReservationHasNotExpiredShouldSucceed()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(100));
        service.ExpireDueReservations();

        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.True(reservationToConfirm.Success);
        Assert.Equal(ReservationStatus.Confirmed, reservationToConfirm.Reservation?.Status);
    }

    [Fact]
    public void ConfirmWhenReservationIsAtExpiryTimeShouldFail()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(120));
        service.ExpireDueReservations();

        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.False(reservationToConfirm.Success);
        Assert.Equal("INVALID_CONFIRM_RESERVATION_LIFECYCLE", reservationToConfirm.ErrorCode);
    }

    [Fact]
    public void ConfirmWhenReservationHasExpiredShouldFail()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(130));
        service.ExpireDueReservations();

        var reservationToConfirm = service.Confirm(result.Reservation?.Id ?? Guid.Empty);

        Assert.NotNull(reservationToConfirm);
        Assert.False(reservationToConfirm.Success);
        Assert.Equal("INVALID_CONFIRM_RESERVATION_LIFECYCLE", reservationToConfirm.ErrorCode);
    }

    [Fact]
    public void ExpireWhenReservationIsBeforeExpiryTimeShouldRemainActive()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(100));
        service.ExpireDueReservations();

        var inventorySnapshot = service.GetSnapshot();

        Assert.Equal(1, inventorySnapshot.Reservations.Count(r => r.Id == result.Reservation?.Id && r.Status == ReservationStatus.Active));
    }


    [Fact]
    public void ExpireWhenReservationReachesExpiryTimeShouldSetStatusToExpired()
    {
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 14,
                0, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(1, timeProvider: fakeTimeProvider, holdDuration: TimeSpan.FromMinutes(2));

        var result = service.Reserve("user-001", 1);

        Assert.NotNull(result.Reservation);
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Active, result.Reservation?.Status);

        // Waiting for the reservation to expire (simulate expiration)
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(130));
        service.ExpireDueReservations();

        var inventorySnapshot = service.GetSnapshot();

        Assert.Equal(1, inventorySnapshot.Reservations.Count(r => r.Id == result.Reservation?.Id && r.Status == ReservationStatus.Expired));
    }
}
