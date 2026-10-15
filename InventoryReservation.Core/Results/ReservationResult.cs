using InventoryReservation.Core.Models;

namespace InventoryReservation.Core.Results;

public sealed record ReservationResult(
    bool Success,
    string? ErrorCode,
    string? Message,
    Reservation? Reservation)
{
    public static ReservationResult Ok(Reservation reservation) =>
        new(true, null, null, reservation);

    public static ReservationResult Fail(string errorCode, string message) =>
        new(false, errorCode, message, null);
}
