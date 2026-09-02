import { useState } from "react";
import {
  Target,
  Heart,
  Lightbulb,
  Users,
  Sparkles,
  Bot,
} from "lucide-react";
import { PageHeader } from "@/components/system/PageHeader";
import { cn } from "@/lib/utils";
import {
  metrics,
  feedback,
  roleHub,
  agents,
  insightsReadiness,
  enterpriseRoleCoverage,
  initiativeOutcomes,
  teamOkrs,
} from "@/data/mock";

type TabId =
  | "overview"
  | "adoption"
  | "readiness"
  | "sentiment"
  | "audience"
  | "ai-agents"
  | "impact"
  | "okrs";

const TABS: { id: TabId; label: string }[] = [
  { id: "overview", label: "Overview" },
  { id: "adoption", label: "Adoption" },
  { id: "readiness", label: "Readiness" },
  { id: "sentiment", label: "Sentiment" },
  { id: "audience", label: "Audience & Roles" },
  { id: "ai-agents", label: "AI & Agents" },
  { id: "impact", label: "Impact & Outcomes" },
  { id: "okrs", label: "OKRs" },
];

function ReadinessStats() {
  const stats = [
    { icon: Target, accent: "bg-indigo-500/10 text-indigo-600", value: insightsReadiness.activeInitiatives, label: "Active Initiatives", caption: "Currently active" },
    { icon: Heart, accent: "bg-rose-500/10 text-rose-600", value: insightsReadiness.atRisk, label: "At Risk", caption: "Needs escalation" },
    { icon: Lightbulb, accent: "bg-violet-500/10 text-violet-600", value: insightsReadiness.needsAttention, label: "Needs Attention", caption: "Requires intervention" },
    { icon: Users, accent: "bg-fuchsia-500/10 text-fuchsia-600", value: enterpriseRoleCoverage.length, label: "Enterprise Roles Covered", caption: `Of ${enterpriseRoleCoverage.length} configured roles` },
  ];

  return (
    <section className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {stats.map((s) => {
        const Icon = s.icon;
        return (
          <div key={s.label} className="glass rounded-2xl p-4">
            <div className={cn("size-9 rounded-xl grid place-items-center", s.accent)}>
              <Icon className="size-4" />
            </div>
            <div className="mt-3 text-2xl font-semibold tracking-tight">{s.value}</div>
            <div className="text-[13px] font-medium mt-0.5">{s.label}</div>
            <div className="text-[11px] text-muted-foreground mt-1">{s.caption}</div>
          </div>
        );
      })}
    </section>
  );
}

function AdoptionOutcomeMetrics() {
  return (
    <div className="glass rounded-2xl p-5">
      <h3 className="font-semibold">Adoption & Outcome Metrics</h3>
      <p className="text-xs text-muted-foreground mt-0.5">
        Mock metrics retained from the prototype until a governed analytics source is connected.
      </p>
      <div className="mt-4 grid sm:grid-cols-3 gap-x-6 gap-y-5">
        {metrics.map((m) => (
          <div key={m.id}>
            <div className="text-[11px] text-muted-foreground">{m.category}</div>
            <div className="mt-1 text-2xl font-semibold tracking-tight">
              {m.value.toLocaleString()}{" "}
              <span className="text-sm font-normal text-muted-foreground">{m.unit}</span>
            </div>
            <div className="text-[13px] font-medium">{m.name}</div>
            <div className="text-[11px] text-emerald-600 font-medium mt-0.5">+{m.trend}% trend</div>
          </div>
        ))}
      </div>
    </div>
  );
}

function ImpactedEnterpriseRoles() {
  return (
    <div className="glass rounded-2xl p-5">
      <h3 className="font-semibold">Impacted Enterprise Roles</h3>
      <p className="text-xs text-muted-foreground mt-0.5">
        Initiative coverage and high-impact exposure by role.
      </p>
      <div className="mt-4 grid sm:grid-cols-2 lg:grid-cols-4 gap-x-6 gap-y-5">
        {enterpriseRoleCoverage.map((r) => (
          <div key={r.code}>
            <div className="text-sm font-semibold">{r.code}</div>
            <div className="text-[11px] text-muted-foreground">{r.name}</div>
            <div className="mt-2 flex items-center justify-between text-[12px]">
              <span>{r.initiativeCount} initiatives</span>
              <span className={cn("font-semibold", r.highImpactCount > 0 ? "text-rose-600" : "text-muted-foreground")}>
                {r.highImpactCount} high impact
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function RoleHubAdoption() {
  return (
    <div className="glass rounded-2xl p-5">
      <h3 className="font-semibold">Role Hub Adoption</h3>
      <div className="mt-4 grid sm:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-5">
        {roleHub.map((r) => (
          <div key={r.role}>
            <div className="text-[13px] font-medium">{r.role}</div>
            <div className="text-2xl font-semibold tracking-tight mt-1">{r.adoption}%</div>
            <div className="text-[11px] text-muted-foreground mt-0.5">
              Adoption · {r.satisfaction}% satisfaction
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function SentimentSignals() {
  return (
    <div className="glass rounded-2xl p-5">
      <h3 className="font-semibold">Sentiment Signals</h3>
      <div className="mt-4 grid sm:grid-cols-2 gap-x-6 gap-y-5">
        {feedback.map((f) => (
          <div key={f.id}>
            <div className="text-[11px] text-muted-foreground">{f.source} · {f.date}</div>
            <div className="text-[13px] font-semibold mt-1">{f.theme}</div>
            <div className="text-sm text-muted-foreground mt-0.5">&ldquo;{f.quote}&rdquo;</div>
            <div className="text-[11px] text-muted-foreground mt-1">
              {f.volume} mentions · sentiment {Math.round(f.sentiment * 100)}%
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function AiAgentAnalytics() {
  return (
    <div className="glass rounded-2xl p-5">
      <h3 className="font-semibold">AI & Agent Analytics</h3>
      <div className="mt-4 grid sm:grid-cols-2 lg:grid-cols-3 gap-x-6 gap-y-5">
        {agents.map((a) => (
          <div key={a.agent}>
            <div className="flex items-center gap-2">
              <div className="size-7 rounded-lg bg-indigo-500/10 grid place-items-center text-indigo-600 shrink-0">
                <Bot className="size-3.5" />
              </div>
              <div className="text-[13px] font-semibold">{a.agent}</div>
            </div>
            <div className="mt-2 grid grid-cols-3 gap-2 text-center">
              <div>
                <div className="font-semibold">{a.invocations.toLocaleString()}</div>
                <div className="text-[10px] text-muted-foreground">Uses</div>
              </div>
              <div>
                <div className="font-semibold">{a.successRate}%</div>
                <div className="text-[10px] text-muted-foreground">Success</div>
              </div>
              <div>
                <div className="font-semibold">{a.savedHours.toLocaleString()}</div>
                <div className="text-[10px] text-muted-foreground">Hours</div>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function InitiativeOutcomes() {
  return (
    <div className="glass rounded-2xl p-5">
      <h3 className="font-semibold">Initiative Outcomes</h3>
      <ul className="mt-4 divide-y divide-black/5">
        {initiativeOutcomes.map((o) => (
          <li key={o.id} className="py-3 flex items-center gap-3">
            <div className="flex-1 min-w-0">
              <div className="text-sm font-medium">{o.name}</div>
              <div className="text-[12px] text-muted-foreground mt-0.5">{o.outcome}</div>
            </div>
            <span className="text-sm font-semibold shrink-0">{o.progress}%</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function OkrsView() {
  return (
    <div className="space-y-4">
      {teamOkrs.map((okr) => (
        <div key={okr.id} className="glass rounded-2xl p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h3 className="font-semibold">{okr.objective}</h3>
              <p className="text-[11px] text-muted-foreground mt-0.5">
                {okr.quarter} · Owner {okr.owner} · Assigned by {okr.assignedBy}
              </p>
            </div>
          </div>
          <ul className="mt-4 space-y-3">
            {okr.keyResults.map((kr) => (
              <li key={kr.id}>
                <div className="flex items-center justify-between text-[13px]">
                  <span>{kr.description}</span>
                  <span className="font-semibold shrink-0 ml-3">{kr.progress}%</span>
                </div>
                <div className="mt-1.5 h-1.5 rounded-full bg-muted overflow-hidden">
                  <div className="h-full bg-copilot-gradient" style={{ width: `${kr.progress}%` }} />
                </div>
              </li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
}

export default function AnalyticsPage() {
  const [tab, setTab] = useState<TabId>("overview");

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Intelligence"
        title="Insights"
        description="A single view across adoption, readiness, sentiment, audience impact, AI/Agent analytics and outcomes."
      />

      <div className="glass rounded-2xl p-1.5 flex flex-wrap gap-1">
        {TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={cn(
              "text-[13px] font-medium px-3.5 py-1.5 rounded-xl transition-colors",
              tab === t.id
                ? "bg-copilot-gradient text-white shadow-sm"
                : "text-muted-foreground hover:text-foreground hover:bg-white/60"
            )}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === "overview" && (
        <div className="space-y-4">
          <ReadinessStats />
          <AdoptionOutcomeMetrics />
          <ImpactedEnterpriseRoles />
        </div>
      )}

      {tab === "adoption" && (
        <div className="space-y-4">
          <AdoptionOutcomeMetrics />
          <RoleHubAdoption />
        </div>
      )}

      {tab === "readiness" && <ReadinessStats />}

      {tab === "sentiment" && <SentimentSignals />}

      {tab === "audience" && <ImpactedEnterpriseRoles />}

      {tab === "ai-agents" && <AiAgentAnalytics />}

      {tab === "impact" && (
        <div className="space-y-4">
          <AdoptionOutcomeMetrics />
          <InitiativeOutcomes />
        </div>
      )}

      {tab === "okrs" && (
        <div>
          <div className="flex items-center gap-2 mb-1">
            <Sparkles className="size-3.5 text-fuchsia-500" />
            <p className="text-xs text-muted-foreground">
              Mock OKRs — standing in until the team's real objectives are connected.
            </p>
          </div>
          <div className="mt-3">
            <OkrsView />
          </div>
        </div>
      )}
    </div>
  );
}
