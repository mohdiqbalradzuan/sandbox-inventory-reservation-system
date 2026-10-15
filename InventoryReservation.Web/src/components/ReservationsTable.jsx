import { useEffect, useState } from "react";

import PropTypes from "prop-types";

ReservationsTable.propTypes = {
  inventorySnapshot: PropTypes.object,
};

function formatRemaining(seconds) {
  const minutes = Math.floor(seconds / 60);
  const secs = seconds % 60;

  return `${String(minutes).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
}

function ReservationRemaining({ reservation }) {
  const [now, setNow] = useState(0);

  useEffect(() => {
    if (reservation.status !== "Active") {
      return;
    }

    const update = () => {
      setNow(Date.now());
    };

    const initialTimer = setTimeout(update, 0);

    const interval = setInterval(update, 1000);

    return () => {
      clearTimeout(initialTimer);
      clearInterval(interval);
    };
  }, [reservation.status, reservation.expiresAtUtc]);

  if (reservation.status !== "Active") {
    return "-";
  }

  if (now === 0) {
    return "--:--";
  }

  const expiresAt = new Date(reservation.expiresAtUtc).getTime();

  const remainingSeconds = Math.max(0, Math.ceil((expiresAt - now) / 1000));

  return formatRemaining(remainingSeconds);
}

function ReservationsTable(props) {
  const { inventorySnapshot } = props;

  return (
    <section className="card">
      <h2>Reservations</h2>
      <div className="table-responsive">
        <table className="table table-dark table-hover align-middle mb-0">
          <thead>
            <tr>
              <th>User</th>
              <th>Status</th>
              <th>Remaining</th>
            </tr>
          </thead>

          <tbody>
            {inventorySnapshot?.reservations.length === 0 ? (
              <tr>
                <td colSpan="3" className="text-center text-secondary py-4">
                  No reservations yet.
                </td>
              </tr>
            ) : (
              inventorySnapshot?.reservations.map((reservation) => (
                <tr key={reservation.id}>
                  <td>{reservation.userId}</td>

                  <td>
                    <span
                      className={`badge ${
                        reservation.status === "Active"
                          ? "bg-primary"
                          : reservation.status === "Confirmed"
                            ? "bg-success"
                            : reservation.status === "Cancelled"
                              ? "bg-warning text-dark"
                              : "bg-secondary"
                      }`}
                    >
                      {reservation.status}
                    </span>
                  </td>

                  <td>
                    <ReservationRemaining reservation={reservation} />
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default ReservationsTable;
