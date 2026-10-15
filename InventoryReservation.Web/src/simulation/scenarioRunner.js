// src/simulation/scenarioRunner.js

import {
  reserveItem,
  confirmReservation,
  cancelReservation,
} from "../services/inventoryApi.js";

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

export async function runScenario({
  requestCount = 100,
  confirmPercent = 60,
  cancelPercent = 20,
  onEvent = console.debug,
} = {}) {
  if (!Number.isInteger(requestCount) || requestCount < 1) {
    throw new Error("Request count must be a positive integer.");
  }

  if (
    !Number.isFinite(confirmPercent) ||
    !Number.isFinite(cancelPercent) ||
    confirmPercent < 0 ||
    cancelPercent < 0 ||
    confirmPercent + cancelPercent > 100
  ) {
    throw new Error("Confirm/cancel percentages must total 100 or less.");
  }

  const runId = crypto.randomUUID();

  const report = (event) => onEvent({ runId, ...event });

  // Log actual HTTP results separately from planned actions.
  async function perform(action, details, request) {
    let response;

    try {
      response = await request();
    } catch (error) {
      report({
        ...details,
        action,
        outcome: "RequestError",
        message: error instanceof Error ? error.message : String(error),
      });

      return null;
    }

    const { ok, status, body } = response;

    // Do not count server errors or unexpected responses
    // as normal stock/lifecycle rejections.
    if (
      status >= 500 ||
      typeof body?.success !== "boolean" ||
      (ok && body.success && !body.reservation?.id)
    ) {
      report({
        ...details,
        action,
        outcome: "RequestError",
        message: `Unexpected API response (HTTP ${status}).`,
      });

      return null;
    }

    const succeeded = ok && body.success;

    report({
      ...details,
      reservationId: body.reservation?.id ?? details.reservationId,
      action,
      outcome: succeeded ? "Succeeded" : "Rejected",
      errorCode: body.errorCode,
      message: body.message,
    });

    return succeeded ? body.reservation : null;
  }

  // Phase 1: reserve one unit for each simulated user.
  const results = await Promise.all(
    Array.from({ length: requestCount }, (_, index) => {
      const userId = `sim-${runId}-${index + 1}`;

      return perform("reserve", { userId }, () => reserveItem(userId, 1));
    }),
  );

  // Only successful reservations receive follow-up actions.
  const reservations = results.filter(Boolean);

  // Phase 2: choose one action per successful reservation.
  await Promise.all(
    reservations.map(async (reservation) => {
      const details = {
        userId: reservation.userId,
        reservationId: reservation.id,
      };

      const roll = Math.random() * 100;

      let action = "abandon";

      if (roll < confirmPercent) {
        action = "confirm";
      } else if (roll < confirmPercent + cancelPercent) {
        action = "cancel";
      }

      if (action === "abandon") {
        report({
          ...details,
          action,
          outcome: "WaitingForExpiry",
        });

        // Do not cancel or expire it from the browser.
        return;
      }

      // Simulate the user taking 1–10 seconds to decide.
      // Random delay between 30,000 and 50,000 milliseconds
      const minMs = 20 * 1000; // 30,000 ms
      const maxMs = 40 * 1000; // 50,000 ms
      const delayMs = Math.floor(Math.random() * (maxMs - minMs + 1)) + minMs;

      report({
        ...details,
        action,
        outcome: "Scheduled",
        delayMs,
      });

      await delay(delayMs);

      await perform(action, details, () =>
        action === "confirm"
          ? confirmReservation(reservation.id)
          : cancelReservation(reservation.id),
      );
    }),
  );

  // Abandoned reservations may still be Active at this point.
  return {
    runId,
    reservationIds: reservations.map((r) => r.id),
  };
}
