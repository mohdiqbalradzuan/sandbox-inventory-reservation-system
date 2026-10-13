namespace InventoryReservation.Api.Contracts;

public sealed record ReserveItemRequest(string UserId, int Quantity);
