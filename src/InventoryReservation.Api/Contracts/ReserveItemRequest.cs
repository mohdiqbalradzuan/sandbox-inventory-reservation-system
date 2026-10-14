using System.Text.Json.Serialization;

namespace InventoryReservation.Api.Contracts;

public class ReserveItemRequest()
{
    [JsonRequiredAttribute]
    public required string UserId { get; set; }

    [JsonRequiredAttribute]
    public required int Quantity { get; set; }
}

