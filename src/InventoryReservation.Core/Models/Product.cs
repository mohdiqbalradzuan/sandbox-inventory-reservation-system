namespace InventoryReservation.Core.Models;

public sealed class Product
{
    public Guid Id { get; init; }
    public required string Sku { get; init; }
    public required string DisplayName { get; init; }
    public int TotalStock { get; init; }
}
