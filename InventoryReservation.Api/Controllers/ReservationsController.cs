using InventoryReservation.Api.Contracts;
using InventoryReservation.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryReservation.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController(IInventoryService inventoryService) : ControllerBase
{
    [HttpPost]
    public IActionResult Reserve([FromBody] ReserveItemRequest request)
    {
        var result = inventoryService.Reserve(request.UserId, request.Quantity);

        if (!result.Success)
        {
            return result.ErrorCode == "INSUFFICIENT_STOCK"
                ? Conflict(result)
                : BadRequest(result);
        }

        return Created($"/api/reservations/{result.Reservation!.Id}", result);
    }

    [HttpPost("{id}/confirm")]
    public IActionResult Confirm(Guid id)
    {
        var result = inventoryService.Confirm(id);

        if (!result.Success)
        {
            return result.ErrorCode == "INVALID_CONFIRM_RESERVATION_LIFECYCLE"
                ? Conflict(result)
                : BadRequest(result);
        }

        return Ok(result.Reservation);
    }

    [HttpPost("{id}/cancel")]
    public IActionResult Cancel(Guid id)
    {
        var result = inventoryService.Cancel(id);

        if (!result.Success)
        {
            return result.ErrorCode == "INVALID_CANCEL_RESERVATION_LIFECYCLE"
                ? Conflict(result)
                : BadRequest(result);
        }

        return Ok(result.Reservation);
    }
}
