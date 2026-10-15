using InventoryReservation.Core.Models;

namespace InventoryReservation.Api.Realtime.Client;

public interface IInventoryHubClient
{
    Task ItemReserved(Reservation reservation);

    Task ReservationConfirmed(Reservation reservation);

    Task ReservationCancelled(Reservation reservation);

    Task ReservationExpired(int expiredCount);
}
