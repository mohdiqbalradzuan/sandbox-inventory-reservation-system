using InventoryReservation.Core.Services;

namespace InventoryReservation.Api.BackgroundServices;

public partial class ReservationExpiryWorker : BackgroundService
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReservationExpiryWorker> _logger;

    public ReservationExpiryWorker(
        IInventoryService inventoryService,
        ILogger<ReservationExpiryWorker> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var expiredCount =
                _inventoryService.ExpireDueReservations();

            if (expiredCount > 0)
            {
                LogInformationExpiringReservation(
                    "Expired reservation(s).",
                    expiredCount);
            }

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while expiring reservations.")]
    private partial void LogErrorExpiringReservation(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "{message}, Count: {count}")]
    private partial void LogInformationExpiringReservation(string message, int count);
}
