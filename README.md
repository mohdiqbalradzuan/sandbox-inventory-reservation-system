# Inventory Reservation System

A small .NET coding challenge / proof of concept for preventing overselling in a high-concurrency flash-sale scenario.

## 🚀 Features

The implementation follows three progressive levels:

1. **Basic Inventory Reservation**
2. **Reservation Lifecycle & Expiry**
3. **Concurrency Handling**

The main goal is not to build a complete e-commerce application. The focus is correctness of inventory state, reservation lifecycle rules, expiry handling, and concurrency control.

## 🛠️ Tech Stack

- **Framework:** .NET 10.0 
- **Web Framework:** React/Vite
- **Database:** In memory object
- **Testing:** xUnit

## 📋 Prerequisites

Before running this project, ensure you have the following installed:

- [Git](https://git-scm.com) installed on your machine.
- [.NET SDK](https://microsoft.com) (v10.0 or higher)
- [Node.js](https://nodejs.org/) (22.12+ or Node 24 LTS or higher) 
- [Visual Studio 2022 or latest version](https://microsoft.com)
- [VS Code](https://visualstudio.com)

## ⚡ Getting Started

Follow these steps to set up and run the project locally.

### 1. Clone the Repository

```powershell
git clone https://github.com/mohdiqbalradzuan/sandbox-inventory-reservation-system.git
cd sandbox-inventory-reservation-system
```

### 2. Restore Dependencies

```powershell
dotnet restore
```

### 3. Build the Project

```powershell
dotnet build
```

### 4. Run the xUnit Tests

```powershell
dotnet test --no-restore
```

## Projects / Solution Structure

| Project | Purpose |
|---|---|
| InventoryReservation.Core | Business rules, reservation lifecycle and in-memory inventory service |
| InventoryReservation.Tests | - Basic reservation tests<br>- Reservation lifecycle tests<br>- Expiry tests<br>- Concurrency tests |
| InventoryReservation.Api | - HTTP API<br>- Background reservation expiry processing<br>- SignalR support for live dashboard updates |
| InventoryReservation.Web | React/Vite dashboard for simulation and visualization |

The business logic is intentionally kept inside `InventoryReservation.Core` so it can be tested without requiring the API or UI.

---

## Core Business Rules

Available stock is derived from the current reservation state:

```text
Available Stock = Total Stock - Confirmed Sales - Active Reservations
```

Rules:

- Reservations that exceed available stock must fail.
- Only an `Active` reservation holds stock temporarily.
- `Confirmed` means the purchase is completed and cannot be reversed.
- `Cancelled` reservations release their reserved quantity.
- `Expired` reservations release their reserved quantity.
- Only one requester can successfully reserve the last available item.
- Reservation hold duration is configurable; the default is 2 minutes.

Reservation lifecycle:

```text
                    -> Confirmed
Active ------------> Cancelled
                    -> Expired
```

`Confirmed`, `Cancelled`, and `Expired` are terminal outcomes for the current POC.

## Why the Inventory Is In Memory

The coding challenge explicitly asks for inventory to be maintained in memory.

For this 2-3 hour style challenge, the intention is to focus on reservation logic and concurrency rather than spend time configuring a database or distributed infrastructure.

The current implementation therefore keeps:

- one sample `Product`
- its total stock
- a collection of `Reservation` records

inside the running application process.

This is intentional rather than an omission.

A production implementation would normally move authoritative inventory state to persistent shared storage such as SQL Server or another appropriate distributed data store, especially when multiple API instances are involved.

---

# Development Phase

## Level 1 - Basic Inventory Reservation

Level 1 implements the basic reservation operation.

Example scenarios:

```text
Stock = 10
10 concurrent requests, each reserving 1
Expected: 10 successful reservations
```

```text
Stock = 9
10 concurrent requests, each reserving 1
Expected: 9 successful reservations and 1 failure
```

The reservation operation:

1. validates the user and requested quantity
2. enters the inventory critical section
3. calculates available stock
4. rejects the request if there is insufficient stock
5. creates one `Active` reservation
6. returns the reservation result

## Level 2 - Reservation Lifecycle & Expiry

Reservations support these states:

```text
Active
Confirmed
Cancelled
Expired
```

A successful reservation starts as `Active` and receives an expiry time:

```text
ExpiresAtUtc = CreatedAtUtc + HoldDuration
```

The service uses `TimeProvider` rather than directly depending on the system clock.

#### During runtime: `TimeProvider.System` is injected into the service.

#### During unit testing, `FakeTimeProvider` is supplied instead. This makes the 2-minute expiry rule deterministic and allows expiry tests to complete immediately without using real `Task.Delay` calls.

Example test flow:

```text
Reserve item
    -> Active

Advance FakeTimeProvider by 2 minutes

ExpireDueReservations()
    -> Expired
    -> inventory becomes available again
```

`ExpireDueReservations()` contains the business operation only. It does not wait internally.

A background worker is responsible for periodically invoking the method during normal API execution.

This keeps the business rule independently testable.

## Level 3 - Concurrency Handling

The main concurrency scenario is:

```text
Stock = 1
Concurrent reservation attempts = 500

Expected:
1 successful reservation
499 failed reservations
```

The current in-memory strategy uses a single synchronization lock around operations that read or mutate reservation/inventory state.

```csharp
private readonly Lock _sync = new();
```

The important part is that availability checking and reservation creation occur inside the **same critical section**.

Conceptually:

```text
Request A                 Request B
---------                 ---------
acquire lock
check stock = 1
create reservation
release lock
                          acquire lock
                          check stock = 0
                          reject
                          release lock
```

If 500 requests reach the service at approximately the same time, ASP.NET Core may process the requests concurrently, but only one request at a time can execute the protected inventory operation.

The order in which waiting requests acquire the lock is not guaranteed.

---
## Technical Discussions, Limitations or Trade-offs

### Why `lock` was selected

The inventory and reservation list are shared in memory. A single lock keeps the stock check and reservation update together so concurrent requests cannot oversell the same stock.

### Why not `Interlocked` / atomic counters?

`Interlocked` works well for simple counter changes, but reservation handling updates more than one piece of state. Using one lock keeps the logic simple and easier to verify.

### Why not `ConcurrentDictionary`?

A thread-safe collection protects the collection itself, but it does not make the full check-and-reserve flow atomic. The current `List<Reservation>` is safe for this POC because access to it is protected by the same lock.

### ASP.NET Core and Thread Safety

ASP.NET Core can process many requests at the same time, but it does not automatically protect shared application state.

Because the in-memory inventory service is shared by incoming requests, the service is responsible for synchronizing access to its product and reservation data.

---

## Code Quality and CI Checks

SonarAnalyzer was added manually to the project and configured through the shared `build.props` file so code-quality and style rules are checked during the build.

A CI/CD YAML workflow was also added early in the project so build and automated test checks run before further development is added. This gives the project a simple quality gate while the reservation logic, API, SignalR integration, and UI simulation continue to evolve.

## xUnit Automated Tests with `dotnet test --no-restore`

The automated test suite currently covers:

- basic reservation success/failure
- stock availability rules
- Active -> Confirmed transitions
- Active -> Cancelled transitions
- Active -> Expired transitions
- invalid lifecycle transitions
- inventory release after cancellation
- inventory release after expiry
- confirmed purchases remaining allocated
- expiry boundary behavior using `FakeTimeProvider`
- 10 concurrent requests against 10 stock
- 10 concurrent requests against 9 stock
- 500 concurrent requests against 1 stock

The concurrency tests verify both result counts and the inventory business invariant so the system cannot silently oversell.

At the current stage, the project has a passing automated test suite covering Levels 1-3 of the core in-memory implementation.

---

## Why There Is an API Project?

The core challenge can be completed and verified entirely through automated tests.

The API project exists because I also want to demonstrate the reservation engine interactively instead of only showing test output (in future release or stage)

The API will expose the same business operations used by the tests, for example:

```text
Reserve
Confirm
Cancel
Get current inventory / reservation state
```

The API does not contain a separate version of the reservation rules. It calls the same `InventoryReservation.Core` service that is exercised by the unit and concurrency tests.

This means the UI simulation and the automated tests are validating the same implementation through different entry points.

---

## Why SignalR Is Included

SignalR is planned specifically for **visualization and simulation**, not because the core reservation algorithm requires it.

The React dashboard will be able to simulate scenarios such as:

```text
Initial stock: 100
Reservation attempts: 100
Quantity per request: 1
```

and then visualize reservations transitioning between:

```text
Active
Confirmed
Cancelled
Expired
```

SignalR will allow the API to push state changes to the dashboard in real time, for example:

```text
Request received
    -> reservation Active
    -> inventory changes
    -> reservation Confirmed / Cancelled / Expired
    -> updated totals pushed to UI
```

The dashboard is intended to show information such as:

- total stock
- currently available stock
- active reserved quantity
- confirmed quantity
- successful/failed reservation attempts
- current reservation state
- expiry countdown / expiry events

The purpose is similar to the automated tests: make the reservation lifecycle and inventory invariants observable.

The difference is that:

```text
xUnit
    -> provides deterministic automated verification

React + API + SignalR
    -> provides an interactive visual simulation of the same behavior
```

The UI therefore complements the tests rather than replacing them.

---

## Planned UI Simulation

One useful lifecycle scenario will be:

```text
Initial stock       = 100
Reservation attempts = 100
Quantity per request = 1
```

After all reservations succeed, different reservations can be driven down different branches:

```text
60 -> Confirm
20 -> Cancel
20 -> No action / Expire
```

Expected progression:

```text
Initial
Available = 100
Active = 0
Confirmed = 0

After 100 reservations
Available = 0
Active = 100
Confirmed = 0

After 60 confirmations
Available = 0
Active = 40
Confirmed = 60

After 20 cancellations
Available = 20
Active = 20
Confirmed = 60

After remaining 20 expire
Available = 40
Active = 0
Confirmed = 60
```

This gives a visual way to demonstrate that cancellation and expiry release inventory while confirmation does not deduct stock twice.

---

## Current Scope and Production Considerations

The current design intentionally targets a single application process and one in-memory inventory source.

```text
API instance
    -> singleton InMemoryInventoryService
        -> synchronization lock
            -> Product + Reservations
```

This is appropriate for the coding challenge.

It would not, by itself, solve concurrency across multiple API instances because every process would have its own memory and its own lock.

A possible production-oriented extension would be:

```text
Load Balancer
    -> API Instance 1
    -> API Instance 2
    -> API Instance 3
            |
            v
      Shared database
```

At that point, concurrency strategies worth evaluating would include:

- atomic conditional SQL updates
- pessimistic database locking
- optimistic concurrency/version columns
- short database transactions
- distributed cache / coordination where appropriate

That extension is intentionally outside the initial challenge scope.

---
