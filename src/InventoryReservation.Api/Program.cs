using InventoryReservation.Api.Realtime;
using InventoryReservation.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

// One process-wide instance for the in-memory POC.
builder.Services.AddSingleton<IInventoryService, InMemoryInventoryService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<InventoryHub>("/hubs/inventory");

await app.RunAsync();

static partial class Program;
