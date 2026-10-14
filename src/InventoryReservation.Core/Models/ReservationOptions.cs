namespace InventoryReservation.Core.Models;

public class ReservationOptions
{
    public TimeSpan HoldDuration { get; set; }
        = TimeSpan.FromMinutes(2);
}
