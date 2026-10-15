import PropTypes from "prop-types";
import CountdownTimer from "./CountdownTimer";

InventorySummary.propTypes = {
  inventorySnapshot: PropTypes.object,
  holdDuration: PropTypes.number.isRequired,
  simulationStarted: PropTypes.bool,
  countdownEndTime: PropTypes.number,
};

export default function InventorySummary(props) {
  const {
    inventorySnapshot,
    holdDuration,
    simulationStarted = false,
    countdownEndTime,
  } = props;

  let activeReservationCount = inventorySnapshot?.reservations.filter(
    (r) => r.status == 0,
  ).length;

  let confirmedReservationCount = inventorySnapshot?.reservations.filter(
    (r) => r.status == 1,
  ).length;

  let cancelledReservationCount = inventorySnapshot?.reservations.filter(
    (r) => r.status == 2,
  ).length;

  let expiredReservationCount = inventorySnapshot?.reservations.filter(
    (r) => r.status == 3,
  ).length;

  return (
    <section className="card stats">
      <div>
        <span>Total Stock</span>
        <strong>{inventorySnapshot?.totalStock}</strong>
      </div>
      <div>
        <span>Available</span>
        <strong>{inventorySnapshot?.availableStock}</strong>
      </div>
      <div>
        <span>Hold Duration</span>
        <strong>{holdDuration}</strong>
      </div>
      <div>
        <span>Countdown</span>
        <strong>
          {simulationStarted ? (
            <CountdownTimer endTime={countdownEndTime} />
          ) : (
            <>--:--</>
          )}
        </strong>
      </div>
      <div>
        <span>Active Reservations</span>
        <strong>{activeReservationCount}</strong>
      </div>
      <div>
        <span>Confirmed Sales</span>
        <strong>{confirmedReservationCount}</strong>
      </div>
      <div>
        <span>Cancelled Sales</span>
        <strong>{cancelledReservationCount}</strong>
      </div>
      <div>
        <span>Expired Sales</span>
        <strong>{expiredReservationCount}</strong>
      </div>
    </section>
  );
}
