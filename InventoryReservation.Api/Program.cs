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
builder.Services.AddSingleton(new Product
{
    Id = Guid.Parse("8982c329-ab2d-4596-8fcd-6772d3c3b203"),
    Sku = "FLASHSALEPHEADPHONE-001",
    DisplayName = "Flash Sale Headphones",
    TotalStock = 100
});
builder.Services.Configure<ReservationOptions>(
    builder.Configuration.GetSection("Reservation"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCors(options =>
{
    options.AddPolicy("InventoryDashboard", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
var app = builder.Build();

app.UseCors("InventoryDashboard");
app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<InventoryHub>("/hubs/inventory");

await app.RunAsync();

static partial class Program;
