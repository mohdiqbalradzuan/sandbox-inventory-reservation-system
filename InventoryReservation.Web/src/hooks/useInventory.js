import { useEffect, useState } from "react";
import { getInventorySnapshot } from "../services/inventoryApi.js";
import { createInventoryHub } from "../services/inventoryHub.js";

/**
 * SignalR notifies; GET /api/inventory supplies the authoritative inventorySnapshot.
 * All automatic inventorySnapshot reads pass through one refresh queue so an older
 * overlapping HTTP response cannot overwrite a newer one.
 */
export function useInventory() {
  const [inventorySnapshot, setInventorySnapshot] = useState(null);
  const [connectionStatus, setConnectionStatus] = useState("Disconnected");
  const [error, setError] = useState(null);

  useEffect(() => {
    let disposed = false;
    let fetching = false;
    let pending = false;
    let refreshTimer = null;

    async function refresh() {
      if (disposed || fetching) return;
      fetching = true;

      try {
        do {
          pending = false;
          try {
            const nextInventorySnapshot = await getInventorySnapshot();

            if (!disposed) {
              setInventorySnapshot(nextInventorySnapshot);
              setError(null);
            }
          } catch (error) {
            if (!disposed) {
              setError(error instanceof Error ? error.message : String(error));
            }
          }
        } while (pending && !disposed);
      } finally {
        fetching = false;
      }
    }

    function requestRefresh(force = false) {
      if (disposed) return;
      pending = true;

      if (fetching || refreshTimer !== null) return;

      if (force) void refresh();

      // Combine a burst of notifications rather than starting one GET per event.
      refreshTimer = setTimeout(() => {
        refreshTimer = null;
        void refresh();
      }, 50);
    }

    // Initial REST load also works while SignalR is trying to connect.
    requestRefresh(false);

    const hub = createInventoryHub({
      onReservedEvent: (data) => {
        console.debug(`A reservation has been created`, data);
        requestRefresh(false);
      },
      onConfirmedEvent: (data) => {
        console.debug(`A reservation has been confirmed`, data);
        requestRefresh(false);
      },
      onCancelledEvent: (data) => {
        console.debug(`A reservation has been cancelled`, data);
        requestRefresh(false);
      },
      onExpiredEvent: (data) => {
        console.debug(`A count of reservation has expired`, data);
        requestRefresh(true);
      },
      onConnectionChange: (status) => {
        console.debug(`SignalR connection status change`, status);
        if (!disposed) setConnectionStatus(status);
      },
    });

    return () => {
      disposed = true;
      clearTimeout(refreshTimer);

      // An already running GET may finish, but its result is ignored.
      void hub.stop().catch((failure) => {
        console.warn(
          "[InventoryHub] Error while stopping connection:",
          failure,
        );
      });
    };
  }, []);

  return { inventorySnapshot, connectionStatus, error };
}
