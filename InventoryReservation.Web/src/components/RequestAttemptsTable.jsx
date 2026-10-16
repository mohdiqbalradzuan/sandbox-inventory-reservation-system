import PropTypes from "prop-types";

RequestAttemptsTable.propTypes = {
  simulationEvents: PropTypes.object,
};

export default function RequestAttemptsTable(props) {
  const { simulationEvents } = props;

  return (
    <section className="card">
      <h2>Request Attempts</h2>

      <div className="table-responsive">
        <table className="table table-dark table-hover align-middle mb-0">
          <thead>
            <tr>
              <th>User</th>
              <th>Action</th>
              <th>Outcome</th>
              <th>Message</th>
            </tr>
          </thead>

          <tbody>
            {simulationEvents?.length === 0 ? (
              <tr>
                <td colSpan="4" className="text-center text-secondary py-4">
                  No request attempts yet.
                </td>
              </tr>
            ) : (
              simulationEvents?.map((attempt, index) => (
                <tr key={`${attempt.userId}-${index}`}>
                  <td>{attempt.userId}</td>

                  <td className="text-capitalize">{attempt.action}</td>

                  <td>
                    <span
                      className={`badge ${
                        attempt.outcome === "Succeeded"
                          ? "bg-success"
                          : attempt.outcome === "Rejected"
                            ? "bg-danger"
                            : attempt.outcome === "Scheduled"
                              ? "bg-warning text-dark"
                              : "bg-secondary"
                      }`}
                    >
                      {attempt.outcome}
                    </span>
                  </td>

                  <td className="text-secondary">{attempt.message || "-"}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
