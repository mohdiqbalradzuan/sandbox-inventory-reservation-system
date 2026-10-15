import { useEffect, useState } from "react";

function formatCountdown(totalSeconds) {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;

  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(
    2,
    "0",
  )}`;
}

function CountdownTimer({ endTime }) {
  const [now, setNow] = useState(0);

  useEffect(() => {
    if (!endTime) {
      return;
    }

    const updateNow = () => {
      setNow(Date.now());
    };

    const initialTimer = setTimeout(updateNow, 0);
    const interval = setInterval(updateNow, 1000);

    return () => {
      clearTimeout(initialTimer);
      clearInterval(interval);
    };
  }, [endTime]);

  if (!endTime || now === 0) {
    return "--:--";
  }

  const secondsLeft = Math.max(0, Math.ceil((endTime - now) / 1000));

  return formatCountdown(secondsLeft);
}

export default CountdownTimer;
