using InventoryReservation.Core.Models;
using InventoryReservation.Core.Results;
using Microsoft.Extensions.Options;

namespace InventoryReservation.Core.Services;

public sealed class InMemoryInventoryService(
    Product product,
    TimeProvider timeProvider,
    IOptions<ReservationOptions> options)
    : IInventoryService
{
    private readonly Product _product = product;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ReservationOptions _options = options.Value;

    private readonly Lock _sync = new();
    private readonly List<Reservation> _reservations = new();

    public InMemoryInventoryService()
        : this(new Product
        {
            Id = Guid.Parse("8982c329-ab2d-4596-8fcd-6772d3c3b203"),
            Sku = "FLASHSALEPHEADPHONE-001",
            DisplayName = "Flash Sale Headphones",
            TotalStock = 100
        }, TimeProvider.System, new OptionsWrapper<ReservationOptions>(new ReservationOptions() { HoldDuration = TimeSpan.FromMinutes(2) }))
    {
    }

    public InventorySnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return BuildSnapshot();
        }
    }

    public ReservationResult Reserve(string userId, int quantity)
    {
        // 1. Validate userId and quantity.
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ReservationResult.Fail(
                "INVALID_USER",
                "User ID is required.");
        }

        if (quantity <= 0)
        {
            return ReservationResult.Fail(
                "INVALID_QUANTITY",
                "Quantity must be greater than zero.");
        }

        // 2. Enter critical section.
        lock (_sync)
        {
            // 3. Calculate available stock.
            var activeReservedQuantity = _reservations
                .Where(r => r.Status == ReservationStatus.Active)
                .Sum(r => r.Quantity);

            var confirmedQuantity = _reservations
                .Where(r => r.Status == ReservationStatus.Confirmed)
                .Sum(r => r.Quantity);

            var availableStock =
                _product.TotalStock
                - activeReservedQuantity
                - confirmedQuantity;

            // 4. Reject if quantity > available stock.
            if (quantity > availableStock)
            {
                return ReservationResult.Fail(
                    "INSUFFICIENT_STOCK",
                    $"Unable to reserve {quantity} item(s). " +
                    $"Only {availableStock} item(s) available.");
            }

            // 5. Create one Active reservation.
            TimeSpan holdDuration = _options == null ? TimeSpan.FromMinutes(2) : _options.HoldDuration;
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                ProductId = _product.Id,
                UserId = userId,
                Quantity = quantity,
                Status = ReservationStatus.Active,
                CreatedAtUtc = _timeProvider != null ? _timeProvider.GetUtcNow() : DateTimeOffset.UtcNow,
                ExpiresAtUtc = _timeProvider != null ? _timeProvider.GetUtcNow().Add(holdDuration) : DateTimeOffset.UtcNow.Add(holdDuration)
            };

            _reservations.Add(reservation);

            // 6. Return success.
            return ReservationResult.Ok(reservation);
        }
    }

    public ReservationResult Confirm(Guid reservationId)
    {
        lock (_sync)
        {
            // 2. Find the active reservation by ID.
            var reservationToConfirm = _reservations
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservationId == Guid.Empty)
            {
                return ReservationResult.Fail(
                    "INVALID_RESERVATION_ID",
                    "Invalid reservation ID.");
            }

            if (reservationToConfirm is null)
            {
                return ReservationResult.Fail(
                    "RESERVATION_NOT_FOUND",
                    $"No active reservation found with ID {reservationId}.");
            }
            else
            {
                if (reservationToConfirm.Status == ReservationStatus.Cancelled
                || reservationToConfirm.Status == ReservationStatus.Expired)
                {
                    return ReservationResult.Fail(
                        "INVALID_CONFIRM_RESERVATION_LIFECYCLE",
                        "Invalid lifecycle for reservation confirmation. Cancelled or expired reservations cannot be confirmed.");
                }
            }

            // 3. Mark the reservation as Confirmed.
            reservationToConfirm.Status = ReservationStatus.Confirmed;
            reservationToConfirm.CompletedAtUtc = _timeProvider != null ? _timeProvider.GetUtcNow() : DateTimeOffset.UtcNow;

            // 4. Return success.
            return ReservationResult.Ok(reservationToConfirm);
        }
    }

    public ReservationResult Cancel(Guid reservationId)
    {
        // 1. Enter critical section.
        lock (_sync)
        {
            // 2. Find the active reservation by ID.
            var reservationToCancel = _reservations
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservationId == Guid.Empty)
            {
                return ReservationResult.Fail(
                    "INVALID_RESERVATION_ID",
                    "Invalid reservation ID.");
            }

            if (reservationToCancel is null)
            {
                return ReservationResult.Fail(
                    "RESERVATION_NOT_FOUND",
                    $"No active reservation found with ID {reservationId}.");
            }
            else
            {
                if (reservationToCancel.Status != ReservationStatus.Active)
                {
                    return ReservationResult.Fail(
                        "INVALID_CANCEL_RESERVATION_LIFECYCLE",
                        "Invalid lifecycle for reservation cancellation. Only active reservations can be cancelled.");
                }
            }

            // 3. Mark the reservation as Cancelled.
            reservationToCancel.Status = ReservationStatus.Cancelled;

            // 4. Return success.
            return ReservationResult.Ok(reservationToCancel);
        }
    }

    public int ExpireDueReservations()
    {
        lock (_sync)
        {
            var now = _timeProvider != null ? _timeProvider.GetUtcNow() : DateTime.UtcNow;

            var expiredReservations = _reservations
                .Where(x =>
                    x.Status == ReservationStatus.Active &&
                    x.ExpiresAtUtc <= now)
                .ToList();

            foreach (var reservation in expiredReservations)
            {
                reservation.Status = ReservationStatus.Expired;
            }

            return expiredReservations.Count;
        }
    }

    private InventorySnapshot BuildSnapshot()
    {
        var active = _reservations
            .Where(x => x.Status == ReservationStatus.Active)
            .Sum(x => x.Quantity);

        var confirmed = _reservations
            .Where(x => x.Status == ReservationStatus.Confirmed)
            .Sum(x => x.Quantity);

        var available = _product.TotalStock - active - confirmed;

        var reservations = _reservations
            .Select(x => new Reservation
            {
                Id = x.Id,
                ProductId = x.ProductId,
                UserId = x.UserId,
                Quantity = x.Quantity,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                CompletedAtUtc = x.CompletedAtUtc
            })
            .ToList();

        return new InventorySnapshot(
            _product.Id,
            _product.Sku,
            _product.DisplayName,
            _product.TotalStock,
            available,
            active,
            confirmed,
            reservations);
    }
}
