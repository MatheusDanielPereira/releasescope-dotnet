import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  Box,
  Braces,
  CheckCircle2,
  CircleDot,
  Clock3,
  GitCommitHorizontal,
  LayoutDashboard,
  Radio,
  Search,
  ServerCog,
  ShieldCheck,
  Sparkles
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
        <header>
          <div>
            <p className="eyebrow">PRODUCTION CONTROL CENTER</p>
            <h1>Deployment overview</h1>
            <p>Explainable risk intelligence for every release.</p>
          </div>
          <div className="header-actions">
            <span className="live"><i /> Live</span>
            <button><Sparkles size={17} /> Simulate deployment</button>
            <div className="avatar">MP</div>
          </div>
        </header>

        <section className="metrics-grid">
          <Metric label="Deployments today" value={dashboard.deploymentsToday} delta="18%" positive icon={<GitCommitHorizontal />} />
          <Metric label="Average risk score" value={dashboard.averageRisk} suffix="/ 100" delta="7.4" icon={<ShieldCheck />} />
          <Metric label="Blocked deployments" value={dashboard.blockedDeployments} delta="1 today" icon={<AlertTriangle />} />
          <Metric label="Change failure rate" value={`${dashboard.changeFailureRate}%`} delta="3.2%" positive icon={<Activity />} />
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

        <section className="flow-strip">
          <div><Braces size={20} /><span><strong>GitHub webhook</strong><small>Signature verified</small></span></div>
          <i />
          <div><Activity size={20} /><span><strong>Signal analysis</strong><small>9 factors evaluated</small></span></div>
          <i />
          <div><ShieldCheck size={20} /><span><strong>Policy decision</strong><small>Explainable output</small></span></div>
          <i />
          <div><Radio size={20} /><span><strong>Live telemetry</strong><small>OpenTelemetry ready</small></span></div>
        </section>
      </main>
    </div>
  );
}

function Metric({ label, value, suffix, delta, positive, icon }: { label: string; value: string | number; suffix?: string; delta: string; positive?: boolean; icon: React.ReactNode }) {
  return (
    <article className="metric-card">
      <div className="metric-icon">{icon}</div>
      <span>{label}</span>
      <strong>{value} <small>{suffix}</small></strong>
      <em className={positive ? "positive" : "neutral"}>{positive ? <ArrowDownRight size={14} /> : <ArrowUpRight size={14} />}{delta}</em>
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
      <div className={`decision ${deployment.risk.isBlocked ? "blocked" : "approved"}`}>
        {deployment.risk.isBlocked ? <AlertTriangle size={18} /> : <CheckCircle2 size={18} />}
        <span><strong>{deployment.risk.isBlocked ? "Deployment blocked" : "Deployment can proceed"}</strong><small>{deployment.risk.isBlocked ? "Resolve critical signals before retrying." : "Risk is within the configured policy."}</small></span>
      </div>
      <div className="factor-list">
        <h3>Contributing factors</h3>
        {deployment.risk.factors.length ? deployment.risk.factors.slice(0, 5).map((factor) => (
          <div className="factor" key={factor.code} title={factor.explanation}>
            <span>{factor.label}</span>
            <b className={factor.impact < 0 ? "protective" : "risky"}>{factor.impact > 0 ? "+" : ""}{factor.impact}</b>
          </div>
        )) : <p className="empty-factors">No material risk factors detected.</p>}
      </div>
    </>
  );
}

export default App;
