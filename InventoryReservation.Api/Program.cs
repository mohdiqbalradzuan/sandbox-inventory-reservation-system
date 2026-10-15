using InventoryReservation.Api.BackgroundServices;
using InventoryReservation.Api.Realtime;
using InventoryReservation.Core.Models;
using InventoryReservation.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

// One process-wide instance for the in-memory POC.
builder.Services.AddHostedService<ReservationExpiryWorker>();
builder.Services.AddSingleton<IInventoryService, InMemoryInventoryService>();
builder.Services.Configure<ReservationOptions>(
    builder.Configuration.GetSection("Reservation"));
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<InventoryHub>("/hubs/inventory");

await app.RunAsync();

static partial class Program;
