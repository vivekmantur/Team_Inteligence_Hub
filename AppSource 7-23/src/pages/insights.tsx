import { useState } from "react";
import {
  Target,
  Heart,
  Lightbulb,
  Users,
  Bot,
} from "lucide-react";
import { PageHeader } from "@/components/system/PageHeader";
import { cn } from "@/lib/utils";
import { metrics, agents } from "@/data/mock";
import {
  useReadinessInsights,
  enterpriseRoleToLabel,
  type ReadinessInsights,
} from "@/hooks/use-initiatives-api";

type TabId = "overview" | "readiness" | "audience" | "ai-agents";

const TABS: { id: TabId; label: string }[] = [
  { id: "overview", label: "Overview" },
  { id: "readiness", label: "Readiness" },
  { id: "audience", label: "Audience & Roles" },
  { id: "ai-agents", label: "AI & Agents" },
];

function ReadinessStats({
  insights,
  isLoading,
}: {
  insights: ReadinessInsights | undefined;
  isLoading: boolean;
}) {
  if (isLoading || !insights) {
    return (
      <section className="glass rounded-2xl p-8 text-center text-sm text-muted-foreground">
        Loading readiness data…
      </section>
    );
  }

  const stats = [
    { icon: Target, accent: "bg-indigo-500/10 text-indigo-600", value: insights.activeInitiatives, label: "Active Initiatives", caption: "Currently active" },
    { icon: Heart, accent: "bg-rose-500/10 text-rose-600", value: insights.atRisk, label: "At Risk", caption: "Needs escalation" },
    { icon: Lightbulb, accent: "bg-violet-500/10 text-violet-600", value: insights.needsAttention, label: "Needs Attention", caption: "Requires intervention" },
    { icon: Users, accent: "bg-fuchsia-500/10 text-fuchsia-600", value: insights.enterpriseRolesCovered, label: "Enterprise Roles Covered", caption: `Of ${insights.totalEnterpriseRoles} configured roles` },
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

function ImpactedEnterpriseRoles({
  insights,
  isLoading,
}: {
  insights: ReadinessInsights | undefined;
  isLoading: boolean;
}) {
  if (isLoading || !insights) {
    return (
      <div className="glass rounded-2xl p-8 text-center text-sm text-muted-foreground">
        Loading role coverage…
      </div>
    );
  }

  return (
    <div className="glass rounded-2xl p-5">
      <h3 className="font-semibold">Impacted Enterprise Roles</h3>
      <p className="text-xs text-muted-foreground mt-0.5">
        Initiative coverage and high-impact exposure by role.
      </p>
      <div className="mt-4 grid sm:grid-cols-2 lg:grid-cols-4 gap-x-6 gap-y-5">
        {insights.roleCoverage.map((r) => (
          <div key={r.code}>
            <div className="text-sm font-semibold">{r.code}</div>
            <div className="text-[11px] text-muted-foreground">{enterpriseRoleToLabel(r.code)}</div>
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

export default function InsightsPage() {
  const [tab, setTab] = useState<TabId>("overview");
  const { data: insights, isLoading: isLoadingInsights } = useReadinessInsights();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Intelligence"
        title="Insights"
        description="A single view across readiness, audience impact, and AI/Agent analytics."
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
          <ReadinessStats insights={insights} isLoading={isLoadingInsights} />
          <AdoptionOutcomeMetrics />
          <ImpactedEnterpriseRoles insights={insights} isLoading={isLoadingInsights} />
        </div>
      )}

      {tab === "readiness" && (
        <ReadinessStats insights={insights} isLoading={isLoadingInsights} />
      )}

      {tab === "audience" && (
        <ImpactedEnterpriseRoles insights={insights} isLoading={isLoadingInsights} />
      )}

      {tab === "ai-agents" && <AiAgentAnalytics />}
    </div>
  );
}
