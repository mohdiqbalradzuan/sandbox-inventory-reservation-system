using InventoryReservation.Api.Contracts;
using InventoryReservation.Api.Realtime;
using InventoryReservation.Api.Realtime.Client;
using InventoryReservation.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace InventoryReservation.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController(IInventoryService inventoryService, IHubContext<InventoryHub, IInventoryHubClient> inventoryHub) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Reserve([FromBody] ReserveItemRequest request)
    {
        var result = inventoryService.Reserve(request.UserId, request.Quantity);

        if (!result.Success)
        {
            return result.ErrorCode == "INSUFFICIENT_STOCK"
                ? Conflict(result)
                : BadRequest(result);
        }

        await inventoryHub.Clients.All.ItemReserved(result.Reservation!);
        return Created($"/api/reservations/{result.Reservation!.Id}", result);

    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = inventoryService.Confirm(id);

        if (!result.Success)
        {
            return result.ErrorCode == "INVALID_CONFIRM_RESERVATION_LIFECYCLE"
                ? Conflict(result)
                : BadRequest(result);
        }

        await inventoryHub.Clients.All.ReservationConfirmed(result.Reservation!);
        return Ok(result.Reservation);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = inventoryService.Cancel(id);

        if (!result.Success)
        {
            return result.ErrorCode == "INVALID_CANCEL_RESERVATION_LIFECYCLE"
                ? Conflict(result)
                : BadRequest(result);
        }

        await inventoryHub.Clients.All.ReservationCancelled(result.Reservation!);
        return Ok(result.Reservation);
    }
}
