import { demoDashboard } from "./demoData";
import type { Dashboard } from "./types";

export async function getDashboard(): Promise<Dashboard> {
  try {
    const response = await fetch("/api/dashboard");
    if (!response.ok) {
      throw new Error(`Dashboard request failed with ${response.status}`);
    }

    return (await response.json()) as Dashboard;
  } catch {
    return demoDashboard;
  }
}
