const API_BASE_URL = "https://localhost:7001";

export async function getInventorySnapshot() {
  const response = await fetch(`${API_BASE_URL}/api/inventory`);
  if (!response.ok) throw new Error("Failed to load inventory snapshot.");
  return response.json();
}

export async function reserveItem(userId, quantity = 1) {
  const response = await fetch(`${API_BASE_URL}/api/reservations`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId, quantity }),
  });

  const body = await response.json();
  return { ok: response.ok, status: response.status, body };
}
