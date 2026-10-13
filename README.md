# Inventory Reservation System

Starter skeleton for the flash-sale inventory reservation coding challenge.

## Initial scope

Level 1 focuses on:

- one sample product held in memory
- reserving a positive quantity
- rejecting reservations when stock is unavailable
- unit and concurrency tests
- a small React dashboard for visualising inventory and test scenarios

Level 2 will add reservation states and 2-minute expiry.
Level 3 will increase contention and validate race-condition protection.

## Structure

```text
src/
  InventoryReservation.Core/      Business rules and in-memory inventory service
  InventoryReservation.Api/       HTTP API; later SignalR and expiry worker

tests/
  InventoryReservation.Tests/     Unit, concurrency and API tests

web/
  InventoryReservation.Web/       React/Vite dashboard
```

## Suggested build order

1. Implement `InMemoryInventoryService.Reserve`.
2. Make the basic Level 1 unit tests pass.
3. Add the 10-vs-10 and 10-vs-9 concurrency tests.
4. Expose the inventory through the API.
5. Connect the React dashboard.
6. Add lifecycle/expiry in Level 2.
7. Add the 500-request contention scenario in Level 3.

> Note: the generated skeleton targets .NET 10. The environment used to package this archive did not have the .NET SDK installed, so run `dotnet restore` / `dotnet test` locally after extracting it.
