namespace InventoryReservation.Core.Models;

public sealed class Reservation
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public required string UserId { get; init; }
    public int Quantity { get; init; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; init; }

    // Level 2
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
