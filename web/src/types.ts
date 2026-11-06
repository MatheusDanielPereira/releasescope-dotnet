export type RiskLevel = "Low" | "Moderate" | "High" | "Critical";

export interface RiskFactor {
  code: string;
  label: string;
  impact: number;
  explanation: string;
}

export interface RiskAssessment {
  score: number;
  level: RiskLevel;
  decision: "Allow" | "ReviewRequired" | "Block";
  factors: RiskFactor[];
  isBlocked: boolean;
}

export interface Deployment {
  id: string;
  service: string;
  version: string;
  environment: string;
  commit: string;
  author: string;
  startedAt: string;
  status: string;
  risk: RiskAssessment;
}

export interface Dashboard {
  deploymentsToday: number;
  blockedDeployments: number;
  averageRisk: number;
  changeFailureRate: number;
  deployments: Deployment[];
}
