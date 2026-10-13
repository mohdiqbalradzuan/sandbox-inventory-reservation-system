using InventoryReservation.Core.Services;

namespace InventoryReservation.Api.BackgroundServices;

// Level 2 placeholder. Register this only after ExpireDueReservations is implemented.
public sealed partial class ReservationExpiryWorker(
    IInventoryService inventoryService,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                inventoryService.ExpireDueReservations();
            }
            catch (NotImplementedException)
            {
                // Level 1: expiry is intentionally not implemented yet.
            }
            catch (Exception ex)
            {
                LogErrorExpiringReservation(ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while expiring reservations.")]
    private partial void LogErrorExpiringReservation(Exception ex);
}
