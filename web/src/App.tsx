import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  Box,
  Braces,
  CheckCircle2,
  ChevronRight,
  CircleDot,
  Clock3,
  GitCommitHorizontal,
  LayoutDashboard,
  Radio,
  Search,
  ServerCog,
  ShieldCheck,
  Sparkles,
  X,
  Zap
} from "lucide-react";
import { getDashboard } from "./api";
import type { Dashboard, Deployment, RiskLevel } from "./types";

const riskClass: Record<RiskLevel, string> = {
  Low: "low",
  Moderate: "moderate",
  High: "high",
  Critical: "critical"
};

function relativeTime(value: string): string {
  const minutes = Math.max(1, Math.floor((Date.now() - new Date(value).getTime()) / 60_000));
  if (minutes < 60) return `${minutes}m ago`;
  return `${Math.floor(minutes / 60)}h ago`;
}

function App() {
  const [dashboard, setDashboard] = useState<Dashboard>();
  const [selectedId, setSelectedId] = useState<string>();
  const [filter, setFilter] = useState("");
  const [isSimulating, setIsSimulating] = useState(false);
  const [simulationMessage, setSimulationMessage] = useState<string>();

  useEffect(() => {
    void getDashboard().then((data) => {
      setDashboard(data);
      setSelectedId(data.deployments[1]?.id ?? data.deployments[0]?.id);
    });
  }, []);

  const deployments = useMemo(
    () => dashboard?.deployments.filter((item) =>
      item.service.toLowerCase().includes(filter.toLowerCase())) ?? [],
    [dashboard, filter]
  );

  const selected = dashboard?.deployments.find((item) => item.id === selectedId);

  function simulateDeployment() {
    if (!dashboard || isSimulating) return;

    setIsSimulating(true);
    setSimulationMessage(undefined);

    window.setTimeout(() => {
      const simulated: Deployment = {
        id: `simulation-${Date.now()}`,
        service: "checkout-api",
        version: "v6.2.0-rc.4",
        environment: "Production",
        commit: "7b31e4a",
        author: "ReleaseScope demo",
        startedAt: new Date().toISOString(),
        status: "Needs review",
        risk: {
          score: 58.7,
          level: "High",
          decision: "ReviewRequired",
          isBlocked: false,
          factors: [
            { code: "change-size", label: "Large change surface", impact: 18, explanation: "47 files and 3,120 changed lines increase the review surface." },
            { code: "critical-components", label: "Critical components", impact: 16, explanation: "The release touches payment and order orchestration components." },
            { code: "performance-regression", label: "Latency regression", impact: 9.7, explanation: "Pre-production checks detected a 7.4% latency regression." },
            { code: "rollback-plan", label: "Verified rollback plan", impact: -10, explanation: "A tested rollback path limits the blast radius." }
          ]
        }
      };

      setDashboard({
        ...dashboard,
        deploymentsToday: dashboard.deploymentsToday + 1,
        deployments: [simulated, ...dashboard.deployments]
      });
      setSelectedId(simulated.id);
      setSimulationMessage("New deployment analyzed · Human review required");
      setIsSimulating(false);
    }, 900);
  }

  if (!dashboard) {
    return <div className="loading">Loading deployment intelligence…</div>;
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark"><Radio size={20} /></span>
          <div><strong>ReleaseScope</strong><small>Deployment intelligence</small></div>
        </div>
        <nav>
          <a className="active" href="#dashboard"><LayoutDashboard size={18} /> Overview</a>
          <a href="#deployments"><Box size={18} /> Deployments <span>5</span></a>
          <a href="#services"><ServerCog size={18} /> Services</a>
          <a href="#policies"><ShieldCheck size={18} /> Risk policies</a>
          <a href="#telemetry"><Activity size={18} /> Telemetry</a>
        </nav>
        <div className="sidebar-status">
          <div><CircleDot size={15} /> All systems operational</div>
          <span>Last sync 12 seconds ago</span>
        </div>
      </aside>

      <main>
        <div className="signal-banner">
          <span><Zap size={14} /> LIVE POLICY ENGINE</span>
          <p>Every decision is traceable, deterministic, and ready for human review.</p>
          <a href="#pipeline">Explore the decision pipeline <ChevronRight size={14} /></a>
        </div>
        <header>
          <div>
            <p className="eyebrow">PRODUCTION CONTROL CENTER</p>
            <h1>Deployment overview</h1>
            <p>Explainable risk intelligence for every release.</p>
          </div>
          <div className="header-actions">
            <span className="live"><i /> Live</span>
            <button className={isSimulating ? "simulating" : ""} onClick={simulateDeployment} disabled={isSimulating}>
              <Sparkles size={17} /> {isSimulating ? "Analyzing signals…" : "Simulate deployment"}
            </button>
            <div className="avatar">MP</div>
          </div>
        </header>

        <section className="metrics-grid">
          <Metric label="Deployments today" value={dashboard.deploymentsToday} delta="18%" positive trend={[8, 13, 11, 19, 17, 25, 29]} icon={<GitCommitHorizontal />} />
          <Metric label="Average risk score" value={dashboard.averageRisk} suffix="/ 100" delta="7.4" trend={[36, 42, 38, 34, 37, 33, 31]} icon={<ShieldCheck />} />
          <Metric label="Blocked deployments" value={dashboard.blockedDeployments} delta="1 today" trend={[4, 3, 4, 2, 2, 1, 1]} icon={<AlertTriangle />} />
          <Metric label="Change failure rate" value={`${dashboard.changeFailureRate}%`} delta="3.2%" positive trend={[28, 31, 27, 29, 24, 23, 20]} icon={<Activity />} />
        </section>

        <section className="workspace-grid">
          <div className="panel deployments-panel" id="deployments">
            <div className="panel-heading">
              <div><h2>Recent deployments</h2><p>Live decisions across all environments</p></div>
              <label className="search"><Search size={16} /><input value={filter} onChange={(event) => setFilter(event.target.value)} placeholder="Filter services" /></label>
            </div>
            <div className="table-header"><span>Service</span><span>Environment</span><span>Risk</span><span>Status</span><span>Started</span></div>
            {deployments.map((deployment) => (
              <DeploymentRow
                key={deployment.id}
                deployment={deployment}
                selected={deployment.id === selectedId}
                onSelect={() => setSelectedId(deployment.id)}
              />
            ))}
          </div>

          <div className="panel risk-panel">
            {selected ? <RiskDetail deployment={selected} /> : null}
          </div>
        </section>

        <section className="flow-strip" id="pipeline">
          <div className="flow-step active"><span className="step-number">01</span><Braces size={20} /><span><strong>GitHub webhook</strong><small>HMAC signature verified</small></span></div>
          <i><b /></i>
          <div className="flow-step active"><span className="step-number">02</span><Activity size={20} /><span><strong>Signal analysis</strong><small>9 factors evaluated</small></span></div>
          <i><b /></i>
          <div className="flow-step active"><span className="step-number">03</span><ShieldCheck size={20} /><span><strong>Policy decision</strong><small>Explainable output</small></span></div>
          <i><b /></i>
          <div className="flow-step"><span className="step-number">04</span><Radio size={20} /><span><strong>Live telemetry</strong><small>OpenTelemetry ready</small></span></div>
        </section>

        <footer><span>RELEASESCOPE / ENGINEERING PORTFOLIO</span><p>.NET 10 · React · OWASP · Event-driven architecture</p><span>BUILD 2026.11</span></footer>
      </main>

      {simulationMessage ? (
        <div className="toast" role="status">
          <span><AlertTriangle size={18} /></span>
          <div><strong>Simulation complete</strong><small>{simulationMessage}</small></div>
          <button aria-label="Dismiss notification" onClick={() => setSimulationMessage(undefined)}><X size={16} /></button>
        </div>
      ) : null}
    </div>
  );
}

function Metric({ label, value, suffix, delta, positive, trend, icon }: { label: string; value: string | number; suffix?: string; delta: string; positive?: boolean; trend: number[]; icon: React.ReactNode }) {
  const maximum = Math.max(...trend);
  const minimum = Math.min(...trend);
  const points = trend.map((item, index) => `${(index / (trend.length - 1)) * 100},${32 - ((item - minimum) / Math.max(1, maximum - minimum)) * 26}`).join(" ");
  return (
    <article className="metric-card">
      <div className="metric-icon">{icon}</div>
      <span>{label}</span>
      <strong>{value} <small>{suffix}</small></strong>
      <em className={positive ? "positive" : "neutral"}>{positive ? <ArrowDownRight size={14} /> : <ArrowUpRight size={14} />}{delta}</em>
      <svg className="metric-trend" viewBox="0 0 100 36" preserveAspectRatio="none" aria-hidden="true"><polyline points={points} /></svg>
    </article>
  );
}

function DeploymentRow({ deployment, selected, onSelect }: { deployment: Deployment; selected: boolean; onSelect: () => void }) {
  return (
    <button className={`deployment-row ${selected ? "selected" : ""}`} onClick={onSelect}>
      <span className="service-cell"><i>{deployment.service.slice(0, 2).toUpperCase()}</i><b>{deployment.service}<small>{deployment.version} · {deployment.commit}</small></b></span>
      <span>{deployment.environment}</span>
      <span className={`risk-score ${riskClass[deployment.risk.level]}`}>{deployment.risk.score}</span>
      <span className={`status ${deployment.status.toLowerCase().replace(" ", "-")}`}>{deployment.status}</span>
      <span className="time"><Clock3 size={14} />{relativeTime(deployment.startedAt)}</span>
    </button>
  );
}

function RiskDetail({ deployment }: { deployment: Deployment }) {
  const circumference = 2 * Math.PI * 52;
  const offset = circumference - (deployment.risk.score / 100) * circumference;
  const decisionCopy = deployment.risk.decision === "Block"
    ? { title: "Deployment blocked", detail: "Resolve critical signals before retrying.", className: "blocked", icon: <AlertTriangle size={18} /> }
    : deployment.risk.decision === "ReviewRequired"
      ? { title: "Human review required", detail: "A release owner must validate the highlighted signals.", className: "review", icon: <AlertTriangle size={18} /> }
      : { title: "Deployment can proceed", detail: "Risk is within the configured policy.", className: "approved", icon: <CheckCircle2 size={18} /> };
  return (
    <>
      <div className="panel-heading"><div><h2>Risk analysis</h2><p>{deployment.service} · {deployment.version}</p></div><span className={`level ${riskClass[deployment.risk.level]}`}>{deployment.risk.level}</span></div>
      <div className="risk-hero">
        <svg viewBox="0 0 120 120" role="img" aria-label={`Risk score ${deployment.risk.score}`}>
          <circle className="ring-bg" cx="60" cy="60" r="52" />
          <circle className={`ring-value ${riskClass[deployment.risk.level]}`} cx="60" cy="60" r="52" strokeDasharray={circumference} strokeDashoffset={offset} />
        </svg>
        <div><strong>{deployment.risk.score}</strong><span>/ 100</span></div>
      </div>
      <div className={`risk-scale ${riskClass[deployment.risk.level]}`}><i style={{ width: `${deployment.risk.score}%` }} /><span style={{ left: `${deployment.risk.score}%` }} /></div>
      <div className={`decision ${decisionCopy.className}`}>
        {decisionCopy.icon}
        <span><strong>{decisionCopy.title}</strong><small>{decisionCopy.detail}</small></span>
      </div>
      <div className="factor-list">
        <h3>Contributing factors</h3>
        {deployment.risk.factors.length ? deployment.risk.factors.slice(0, 5).map((factor) => (
          <div className="factor" key={factor.code} title={factor.explanation}>
            <span><i style={{ width: `${Math.min(100, Math.abs(factor.impact) * 4)}%` }} />{factor.label}</span>
            <b className={factor.impact < 0 ? "protective" : "risky"}>{factor.impact > 0 ? "+" : ""}{factor.impact}</b>
          </div>
        )) : <p className="empty-factors">No material risk factors detected.</p>}
      </div>
    </>
  );
}

export default App;
