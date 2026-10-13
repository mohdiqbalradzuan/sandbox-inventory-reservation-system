import InventorySummary from "./components/InventorySummary";
import SimulationControls from "./components/SimulationControls";
import RequestAttemptsTable from "./components/RequestAttemptsTable";
import ReservationsTable from "./components/ReservationsTable";

export default function App() {
  return (
    <main className="page-shell">
      <header>
        <p className="eyebrow">Backend Coding Challenge</p>
        <h1>Inventory Reservation System</h1>
        <p className="subtitle">
          Visual test harness for stock reservation, expiry and concurrency scenarios.
        </p>
      </header>

      <InventorySummary />
      <SimulationControls />

      <section className="grid">
        <RequestAttemptsTable />
        <ReservationsTable />
      </section>
    </main>
  );
}
