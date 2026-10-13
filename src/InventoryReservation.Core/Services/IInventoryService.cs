using InventoryReservation.Core.Models;
using InventoryReservation.Core.Results;

namespace InventoryReservation.Core.Services;

public interface IInventoryService
{
    InventorySnapshot GetSnapshot();

    ReservationResult Reserve(string userId, int quantity);

    // Level 2
    ReservationResult Confirm(Guid reservationId);

    ReservationResult Cancel(Guid reservationId);

    int ExpireDueReservations();
}
