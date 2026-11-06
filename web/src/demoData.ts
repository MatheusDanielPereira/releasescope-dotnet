import type { Dashboard } from "./types";

export const demoDashboard: Dashboard = {
  deploymentsToday: 5,
  blockedDeployments: 1,
  averageRisk: 31.7,
  changeFailureRate: 20,
  deployments: [
    {
      id: "a1",
      service: "identity-api",
      version: "v2.18.0",
      environment: "Production",
      commit: "8fa32c1",
      author: "Maya Chen",
      startedAt: new Date(Date.now() - 8 * 60_000).toISOString(),
      status: "Deploying",
      risk: {
        score: 9.6,
        level: "Low",
        decision: "Allow",
        isBlocked: false,
        factors: [
          { code: "critical-components", label: "Critical components", impact: 6, explanation: "The deployment touches one critical component." },
          { code: "change-size", label: "Change size", impact: 3.7, explanation: "8 files and 214 changed lines increase the review surface." },
          { code: "rollback-plan", label: "Verified rollback plan", impact: -10, explanation: "A tested rollback path limits the blast radius." }
        ]
      }
    },
    {
      id: "b2",
      service: "billing-worker",
      version: "v5.7.3",
      environment: "Production",
      commit: "1bc90de",
      author: "Liam Brooks",
      startedAt: new Date(Date.now() - 27 * 60_000).toISOString(),
      status: "Blocked",
      risk: {
        score: 86.4,
        level: "Critical",
        decision: "Block",
        isBlocked: true,
        factors: [
          { code: "failed-tests", label: "Failed tests", impact: 24, explanation: "2 automated tests are failing." },
          { code: "security-findings", label: "Security findings", impact: 8, explanation: "1 unresolved security finding requires attention." },
          { code: "change-size", label: "Change size", impact: 20, explanation: "34 files and 1,860 changed lines increase the review surface." },
          { code: "performance-regression", label: "Performance regression", impact: 10.8, explanation: "Pre-production checks detected an 18% latency regression." }
        ]
      }
    },
    {
      id: "c3",
      service: "customer-portal",
      version: "v3.11.2",
      environment: "Staging",
      commit: "e32bd77",
      author: "Sofia Alvarez",
      startedAt: new Date(Date.now() - 64 * 60_000).toISOString(),
      status: "Deploying",
      risk: { score: 14.8, level: "Low", decision: "Allow", isBlocked: false, factors: [] }
    },
    {
      id: "d4",
      service: "notification-hub",
      version: "v1.26.0",
      environment: "Production",
      commit: "c7d20a9",
      author: "Noah Williams",
      startedAt: new Date(Date.now() - 122 * 60_000).toISOString(),
      status: "Needs review",
      risk: { score: 43.2, level: "Moderate", decision: "ReviewRequired", isBlocked: false, factors: [] }
    },
    {
      id: "e5",
      service: "reporting-api",
      version: "v4.3.1",
      environment: "Production",
      commit: "42d8ef0",
      author: "Aisha Khan",
      startedAt: new Date(Date.now() - 245 * 60_000).toISOString(),
      status: "Deploying",
      risk: { score: 4.5, level: "Low", decision: "Allow", isBlocked: false, factors: [] }
    }
  ]
};
