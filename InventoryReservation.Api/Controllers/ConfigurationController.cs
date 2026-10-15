using InventoryReservation.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace InventoryReservation.Api.Controllers;

[ApiController]
[Route("api/configurations")]
public sealed class ConfigurationController(IOptions<ReservationOptions> options) : ControllerBase
{
    [HttpGet("hold-duration")]
    public IActionResult GetSnapshot()
    {
        return Ok(options.Value.HoldDuration);
    }
}
