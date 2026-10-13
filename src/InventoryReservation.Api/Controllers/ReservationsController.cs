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

    // Level 2 endpoints can be added here:
    // POST /api/reservations/{id}/confirm
    // POST /api/reservations/{id}/cancel
}
