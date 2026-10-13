using InventoryReservation.Core.Models;
using InventoryReservation.Core.Results;

namespace InventoryReservation.Core.Services;

public sealed class InMemoryInventoryService(Product product) : IInventoryService
{
    private readonly object _sync = new();
    private readonly Product _product = product;
    private readonly List<Reservation> _reservations = [];

    public InMemoryInventoryService()
        : this(new Product
        {
            Id = Guid.Parse("8982c329-ab2d-4596-8fcd-6772d3c3b203"),
            Sku = "FLASHSALEPHEADPHONE-001",
            DisplayName = "Flash Sale Headphones",
            TotalStock = 100
        })
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
        // TODO Level 1:
        // 1. Validate userId and quantity.
        // 2. Enter the critical section.
        // 3. Calculate available stock.
        // 4. Reject if quantity > available stock.
        // 5. Create one Active reservation.
        // 6. Return success.
        throw new NotImplementedException("Implement this using TDD for Level 1.");
    }

    public ReservationResult Confirm(Guid reservationId)
    {
        throw new NotImplementedException("Level 2");
    }

    public ReservationResult Cancel(Guid reservationId)
    {
        throw new NotImplementedException("Level 2");
    }

    public int ExpireDueReservations()
    {
        throw new NotImplementedException("Level 2");
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
