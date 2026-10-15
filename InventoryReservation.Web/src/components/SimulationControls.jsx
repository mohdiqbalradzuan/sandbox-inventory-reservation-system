import { useEffect, useState } from "react";
import { runScenario } from "../simulation/scenarioRunner.js";
import PropTypes from "prop-types";

const defaultSimulationFormData = {
  requestCount: 10,
  confirmedRate: 50,
  cancelledRate: 30,
};

SimulationControls.propTypes = {
  onSimulationStart: PropTypes.func,
};

export default function SimulationControls(props) {
  const { onSimulationStart } = props;

  const [formData, setFormData] = useState(defaultSimulationFormData);
  const [errors, setErrors] = useState({});
  const [simulationEvents, setSimulationEvents] = useState([]);
  const [simulationStart, setSimulationStart] = useState(false);

  useEffect(() => {
    return () => {
      setSimulationStart(false);
    };
  }, []);

  const handleChange = (e) => {
    const { name, value } = e.target;

    setFormData((current) => ({
      ...current,
      [name]: value,
    }));

    if (errors[name]) {
      setErrors((current) => ({
        ...current,
        [name]: null,
      }));
    }
  };

  const getControlClass = (fieldName, additionalClass = "") =>
    [
      "form-control",
      additionalClass,
      errors[fieldName] ? "form-control-error" : "",
    ]
      .filter(Boolean)
      .join(" ");

  const validate = () => {
    const validationErrors = {};

    if (Number(formData.requestCount) === 0) {
      validationErrors.requestCount =
        "Request count must be a positive integer.";
    }

    if (Number(formData.confirmedRate) === 0) {
      validationErrors.confirmedRate =
        "Confirm percentages must be positive integer.";
    }

    if (Number(formData.cancelledRate) === 0) {
      validationErrors.cancelledRate =
        "Cancel percentages must be positive integer.";
    }

    if (
      !validationErrors.confirmedRate &&
      !validationErrors.cancelledRate &&
      (!Number.isFinite(formData.confirmedRate) ||
        !Number.isFinite(formData.cancelledRate) ||
        formData.confirmedRate + formData.cancelledRate > 100)
    ) {
      validationErrors.confirmedRate =
        "Confirm/cancel percentages must total 100 or less.";
      validationErrors.cancelledRate =
        "Confirm/cancel percentages must total 100 or less.";
    }

    setErrors(validationErrors);

    return Object.keys(validationErrors).length === 0;
  };

  const handleStartSimulation = async (e) => {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    setSimulationEvents([]);

    var simulationParams = {
      requestCount: formData.requestCount,
      confirmPercent: formData.confirmedRate,
      cancelPercent: formData.cancelledRate,
      onEvent: (event) => {
        setSimulationEvents((previous) => [...previous, event]);
        console.debug("onEvent: ", event);
        console.debug("onEvent: ", simulationEvents);
      },
    };

    console.debug("Simulated user actions starts:", simulationParams);

    setSimulationStart(true);
    onSimulationStart(true);

    const result = await runScenario(simulationParams);

    console.debug("Simulated user actions finished:", result);

    setSimulationStart(false);
    onSimulationStart(false);
  };

  return (
    <section className="card controls">
      <div>
        <div className="row mb-3">
          <div className="col-sm-3 d-flex align-items-center">
            <h4 className="m-auto">Simulation</h4>
          </div>
          <div className="col-sm-9 align-items-center">
            <div className="m-auto">
              <div className={`live-chip ${simulationStart ? "is-live" : ""}`}>
                <span className="live-dot" />{" "}
                {simulationStart ? "STARTED" : "STOPPED"}
              </div>
            </div>
          </div>
        </div>
        <form id="simulation-form" onSubmit={handleStartSimulation}>
          <div className="form">
            <div className="form-group">
              <label className="form-label" htmlFor="requestCount">
                Request count:
              </label>
              <input
                id="requestCount"
                name="requestCount"
                type="number"
                value={formData.requestCount}
                onChange={handleChange}
                className={getControlClass("requestCount")}
              />

              {errors.requestCount && (
                <div className="form-error">{errors.requestCount}</div>
              )}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="confirmedRate">
                Confirmed Rate (%):
              </label>
              <input
                id="confirmedRate"
                name="confirmedRate"
                type="number"
                value={formData.confirmedRate}
                onChange={handleChange}
                className={getControlClass("confirmedRate")}
              />

              {errors.confirmedRate && (
                <div className="form-error">{errors.confirmedRate}</div>
              )}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="cancelledRate">
                Cancelled Rate (%):
              </label>
              <input
                id="cancelledRate"
                name="cancelledRate"
                type="number"
                value={formData.cancelledRate}
                onChange={handleChange}
                className={getControlClass("cancelledRate")}
              />

              {errors.cancelledRate && (
                <div className="form-error">{errors.cancelledRate}</div>
              )}
            </div>
          </div>
        </form>
      </div>
      <button type="submit" form="simulation-form" className="btn btn-primary">
        Start Simulation
      </button>
    </section>
  );
}
