using InventoryReservation.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryReservation.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetSnapshot()
    {
        return Ok(inventoryService.GetSnapshot());
    }
}
