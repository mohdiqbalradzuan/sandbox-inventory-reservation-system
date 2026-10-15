namespace InventoryReservation.Core.Models;

public sealed record InventorySnapshot(
    Guid ProductId,
    string Sku,
    string DisplayName,
    int TotalStock,
    int AvailableStock,
    int ActiveReservedQuantity,
    int ConfirmedQuantity,
    IReadOnlyList<Reservation> Reservations);
