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
  const [simulationEvents, setSimulationEvents] = useState([]);

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

  const handleSimulationEvent = (event) => {
    setSimulationEvents((previous) => [...previous, event]);
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
        <p className="subtitle">
          <strong>Notes:</strong>
        </p>

        <ul>
          <li>
            The React dashboard and SignalR integration are additional features
            built on top of the original coding challenge requirements.
          </li>
          <li>
            The purpose of the dashboard is to visually demonstrate the same
            behaviours covered by the automated tests, particularly reservation
            lifecycle handling, stock release, expiry, and concurrent
            reservation scenarios.
          </li>
          <li>
            The reservation hold duration and initial stock quantity can be
            configured in{" "}
            <code className="bg-dark text-light p-1 rounded">
              <i>.\InventoryReservation.Api\appsettings.json</i>
            </code>
            . Restart the API application after updating the values.
          </li>
          <li>
            A shorter hold duration & minimal stock quantity is configured here
            for faster API and dashboard simulation. The required 2-minute hold
            duration is still used as the default in the xUnit tests.
          </li>
        </ul>
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

        callbackSimulationEvent={handleSimulationEvent}
      />

      <section className="grid">
        <RequestAttemptsTable simulationEvents={simulationEvents} />
        <ReservationsTable inventorySnapshot={inventorySnapshot} />
      </section>
    </main>
  );
}
