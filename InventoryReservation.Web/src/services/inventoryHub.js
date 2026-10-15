import {
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";

const API_BASE_URL = "https://localhost:55146";

export function createInventoryHub({
  onReservedEvent = console.debug,
  onConfirmedEvent = console.debug,
  onCancelledEvent = console.debug,
  onExpiredEvent = console.debug,
  onConnectionChange = console.debug,
}) {
  const hubConnection = new HubConnectionBuilder()
    .configureLogging(LogLevel.Warning)
    .withUrl(`${API_BASE_URL}/hubs/inventory`, {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect()
    .build();

  if (hubConnection) {
    hubConnection
      .start()
      .then(() => {
        console.debug("Connecting to SignalR");
        onConnectionChange("Connected");

        hubConnection.invoke("GetConnectionId").then((data) => {
          console.debug(`SignalR connected with connection Id:`, data);
          onConnectionChange("Connected");
        });

        hubConnection.on("ItemReserved", (data) => {
          onReservedEvent(data);
        });

        hubConnection.on("ReservationConfirmed", (data) => {
          onConfirmedEvent(data);
        });

        hubConnection.on("ReservationCancelled", (data) => {
          onCancelledEvent(data);
        });

        hubConnection.on("ReservationExpired", (data) => {
          onExpiredEvent(data);
        });
      })
      .catch((e) => {
        onConnectionChange("ConnectionError");
        console.debug("SignalR connection failed: ", e);
      });
  }

  return hubConnection;
}
