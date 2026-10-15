import { useState, useEffect } from "react";

import InventorySummary from "./components/InventorySummary";
import SimulationControls from "./components/SimulationControls";
import RequestAttemptsTable from "./components/RequestAttemptsTable";
import ReservationsTable from "./components/ReservationsTable";

import { useInventory } from "./hooks/useInventory";

import { getHoldDurationConfig } from "./services/inventoryApi";

export default function App() {
  const { inventorySnapshot, connectionStatus, error } = useInventory();

  const [holdDuration, setHoldDuration] = useState("");
  const [simulationStart, setSimulationStart] = useState(false);
  const [countdownEndTime, setCountdownEndTime] = useState(null);

  useEffect(() => {
    getHoldDurationConfig().then((data) => {
      setHoldDuration(data);
    });

    return () => {};
  }, []);

  const handleStartSimulation = () => {
    const holdDurationSeconds = 60;

    setCountdownEndTime(Date.now() + holdDurationSeconds * 1000);

    // start simulation...
  };

  return (
    <main className="page-shell">
      <header>
        <p className="eyebrow">Backend Coding Challenge</p>
        <h1>Inventory Reservation System</h1>
        <p className="subtitle">
          Visual test harness for stock reservation, expiry and concurrency
          scenarios.
        </p>
      </header>
      <div className="row mt-4">
        <div className="col-sm-2 d-flex align-items-center">
          <h4 className="ms-0 m-auto">Live Updates:</h4>
        </div>
        <div className="col-sm-10 align-items-center">
          <div className="m-auto">
            <div
              className={`live-chip ${connectionStatus.toLowerCase() === "connected" ? "is-live" : ""}`}
            >
              <span className="live-dot" /> {connectionStatus.toUpperCase()}
            </div>
          </div>
        </div>
      </div>

      {error && <p role="alert">{error}</p>}

      <InventorySummary
        inventorySnapshot={inventorySnapshot}
        holdDuration={holdDuration}
        simulationStarted={simulationStart}
        countdownEndTime={countdownEndTime}
      />

      <SimulationControls
        onSimulationStart={(value) => {
          setSimulationStart(value);
          handleStartSimulation();
        }}
      />

      <section className="grid">
        <RequestAttemptsTable inventorySnapshot={inventorySnapshot} />
        <ReservationsTable inventorySnapshot={inventorySnapshot} />
      </section>
    </main>
  );
}
