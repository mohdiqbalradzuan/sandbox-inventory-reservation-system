using InventoryReservation.Api.Realtime;
using InventoryReservation.Api.Realtime.Client;
using InventoryReservation.Core.Services;
using Microsoft.AspNetCore.SignalR;

namespace InventoryReservation.Api.BackgroundServices;

public partial class ReservationExpiryWorker : BackgroundService
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReservationExpiryWorker> _logger;
    private readonly IHubContext<InventoryHub, IInventoryHubClient> _inventoryHub;

    public ReservationExpiryWorker(
        IInventoryService inventoryService,
        ILogger<ReservationExpiryWorker> logger,
        IHubContext<InventoryHub, IInventoryHubClient> inventoryHub)
    {
        _inventoryService = inventoryService;
        _logger = logger;
        _inventoryHub = inventoryHub;
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

                await _inventoryHub.Clients.All.ReservationExpired(expiredCount);
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
