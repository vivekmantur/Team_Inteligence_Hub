import { PageHeader } from "@/components/system/PageHeader";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { CheckCircle2, Database, ShieldCheck, Users, Bot, Sparkles } from "lucide-react";

const sources = [
  { name: "Microsoft Fabric", status: "Connected", desc: "Analytics, adoption metrics, agent telemetry" },
  { name: "Viva Insights", status: "Connected", desc: "Sentiment & engagement signals" },
  { name: "SharePoint", status: "Connected", desc: "Deliverables and knowledge library" },
  { name: "Copilot Studio", status: "Connected", desc: "Agent invocations and success rates" },
];

export default function AdminPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Administration"
        title="Settings & data sources"
        description="Configure permissions, data grounding, and Copilot behavior for your team."
      />

      <div className="grid lg:grid-cols-3 gap-4">
        <div className="glass rounded-2xl p-5">
          <div className="flex items-center gap-2 mb-3">
            <div className="size-8 rounded-lg bg-copilot-gradient text-white grid place-items-center">
              <Sparkles className="size-4" />
            </div>
            <h3 className="font-semibold">Copilot behavior</h3>
          </div>
          <ul className="space-y-3 text-sm">
            <SettingRow label="Ground on this quarter only" defaultChecked />
            <SettingRow label="Cite sources in responses" defaultChecked />
            <SettingRow label="Allow drafting external content" />
            <SettingRow label="Prefer story-driven tone" defaultChecked />
          </ul>
        </div>

        <div className="glass rounded-2xl p-5">
          <div className="flex items-center gap-2 mb-3">
            <div className="size-8 rounded-lg bg-gradient-to-br from-emerald-500 to-teal-500 text-white grid place-items-center">
              <ShieldCheck className="size-4" />
            </div>
            <h3 className="font-semibold">Governance</h3>
          </div>
          <ul className="space-y-3 text-sm">
            <SettingRow label="Require review for external posts" defaultChecked />
            <SettingRow label="Redact PII in generated content" defaultChecked />
            <SettingRow label="Log all Copilot prompts" defaultChecked />
            <SettingRow label="Restrict Customer Zero to Published" />
          </ul>
        </div>

        <div className="glass rounded-2xl p-5">
          <div className="flex items-center gap-2 mb-3">
            <div className="size-8 rounded-lg bg-gradient-to-br from-indigo-500 to-fuchsia-500 text-white grid place-items-center">
              <Users className="size-4" />
            </div>
            <h3 className="font-semibold">Team & permissions</h3>
          </div>
          <ul className="space-y-3 text-sm">
            <li className="flex items-center justify-between">
              <span>Admins</span><span className="font-semibold">3</span>
            </li>
            <li className="flex items-center justify-between">
              <span>Editors</span><span className="font-semibold">12</span>
            </li>
            <li className="flex items-center justify-between">
              <span>Contributors</span><span className="font-semibold">28</span>
            </li>
            <li className="flex items-center justify-between">
              <span>Viewers</span><span className="font-semibold">184</span>
            </li>
          </ul>
          <Button className="mt-4 w-full rounded-xl bg-copilot-gradient text-white">Invite teammate</Button>
        </div>
      </div>

      <div className="glass rounded-2xl p-5">
        <div className="flex items-center gap-2 mb-3">
          <div className="size-8 rounded-lg bg-gradient-to-br from-sky-500 to-cyan-500 text-white grid place-items-center">
            <Database className="size-4" />
          </div>
          <h3 className="font-semibold">Connected data sources</h3>
        </div>
        <div className="grid md:grid-cols-2 gap-3">
          {sources.map((s) => (
            <div key={s.name} className="rounded-xl bg-white/70 p-4 flex items-center gap-3">
              <div className="size-10 rounded-xl bg-gradient-to-br from-indigo-500/10 to-fuchsia-500/10 grid place-items-center">
                <Bot className="size-5 text-indigo-600" />
              </div>
              <div className="flex-1">
                <div className="text-sm font-semibold">{s.name}</div>
                <div className="text-[11px] text-muted-foreground">{s.desc}</div>
              </div>
              <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-700 bg-emerald-500/10 rounded-full px-2 py-0.5">
                <CheckCircle2 className="size-3" />
                {s.status}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function SettingRow({ label, defaultChecked = false }: { label: string; defaultChecked?: boolean }) {
  return (
    <li className="flex items-center justify-between gap-3">
      <span>{label}</span>
      <Switch defaultChecked={defaultChecked} />
    </li>
  );
}
