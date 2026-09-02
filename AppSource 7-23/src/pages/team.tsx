import { useMemo, useState } from "react";
import { PageHeader } from "@/components/system/PageHeader";
import { teamMembers, deliverables, initiatives } from "@/data/mock";
import { Sparkles, Award, Plus, Users, FileText, CheckCircle2, Paperclip, Rocket, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { useAddContribution } from "@/components/contribution/ContributionContext";

export default function TeamPage() {
  const { openAddContribution, contributions } = useAddContribution();
  const [initiativeFilter, setInitiativeFilter] = useState<string>("All");
  const [q, setQ] = useState("");

  const filteredContributions = useMemo(
    () =>
      contributions.filter(
        (c) =>
          (initiativeFilter === "All" || c.initiativeId === initiativeFilter) &&
          (q === "" || c.title.toLowerCase().includes(q.toLowerCase()) || c.initiativeName.toLowerCase().includes(q.toLowerCase()))
      ),
    [contributions, initiativeFilter, q]
  );

  // Group contributions by Initiative ("Contributions by Initiative")
  const contributionsByInitiative = useMemo(() => {
    const map = new Map<string, { name: string; count: number }>();
    for (const c of contributions) {
      const cur = map.get(c.initiativeId);
      if (cur) cur.count += 1;
      else map.set(c.initiativeId, { name: c.initiativeName, count: 1 });
    }
    return Array.from(map.entries())
      .map(([id, v]) => ({ id, ...v }))
      .sort((a, b) => b.count - a.count);
  }, [contributions]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Team Contributions"
        description="Track contributions, deliverables, and impact for every team member — across every Initiative."
        actions={
          <>
            <Button variant="outline" className="rounded-xl bg-white/70">
              <Sparkles className="size-4" /> Build summary
            </Button>
            <Button
              onClick={() => openAddContribution()}
              className="rounded-xl bg-copilot-gradient text-white"
            >
              <Plus className="size-4" /> Add Contribution
            </Button>
          </>
        }
      />

      {/* Filter bar */}
      <div className="glass rounded-2xl p-3 flex flex-col md:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="size-4 text-muted-foreground absolute left-3 top-1/2 -translate-y-1/2" />
          <Input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search contributions by title or Initiative…"
            className="h-10 pl-9 bg-white/70 rounded-xl border-white/60"
          />
        </div>
        <div className="flex items-center gap-2">
          <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider hidden sm:inline">Initiative Filter</span>
          <select
            value={initiativeFilter}
            onChange={(e) => setInitiativeFilter(e.target.value)}
            className="h-10 rounded-xl border border-input bg-white/80 px-3 text-[13px] min-w-[220px]"
          >
            <option value="All">All Initiatives</option>
            {initiatives.map((i) => (
              <option key={i.id} value={i.id}>{i.name}</option>
            ))}
          </select>
        </div>
      </div>

      {contributions.length > 0 ? (
        <div className="grid lg:grid-cols-[1.6fr_1fr] gap-4">
          <section className="glass rounded-2xl p-5">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="font-semibold">Recent Contributions</h3>
                <p className="text-xs text-muted-foreground">Freshly captured across your Initiatives</p>
              </div>
              <span className="text-[11px] font-semibold text-emerald-700 bg-emerald-500/10 rounded-full px-2 py-0.5">
                {filteredContributions.length} shown
              </span>
            </div>
            <div className="mt-3 grid md:grid-cols-2 gap-3">
              {filteredContributions.slice(0, 8).map((c) => (
                <div key={c.id} className="rounded-xl bg-white/80 border border-black/5 p-3">
                  <div className="flex items-center gap-2">
                    <div className="size-7 rounded-lg bg-copilot-gradient grid place-items-center text-white">
                      <CheckCircle2 className="size-3.5" />
                    </div>
                    <span className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
                      {c.status === "submitted" ? "Submitted" : "Draft"}
                    </span>
                    <span className="ml-auto text-[10px] text-muted-foreground">{new Date(c.createdAt).toLocaleDateString()}</span>
                  </div>
                  <div className="mt-2 text-[13px] font-semibold leading-tight line-clamp-2">{c.title || "Untitled contribution"}</div>
                  <div className="text-[11px] text-muted-foreground mt-0.5 inline-flex items-center gap-1">
                    <Rocket className="size-3" /> {c.initiativeName}
                  </div>
                  <div className="mt-2 flex items-center gap-3 text-[11px] text-muted-foreground">
                    <span className="inline-flex items-center gap-1"><Users className="size-3" />{c.contributors.length}</span>
                    <span className="inline-flex items-center gap-1"><Paperclip className="size-3" />{c.files.length + c.links.length}</span>
                    <span className="inline-flex items-center gap-1"><FileText className="size-3" />{c.types.length} type{c.types.length === 1 ? "" : "s"}</span>
                  </div>
                </div>
              ))}
            </div>
          </section>

          <section className="glass rounded-2xl p-5">
            <h3 className="font-semibold">Contributions by Initiative</h3>
            <p className="text-xs text-muted-foreground">Where the work landed</p>
            <ul className="mt-3 space-y-2">
              {contributionsByInitiative.length === 0 ? (
                <li className="text-[12px] text-muted-foreground">No contributions yet.</li>
              ) : (
                contributionsByInitiative.map((row) => {
                  const max = Math.max(...contributionsByInitiative.map((r) => r.count));
                  const pct = Math.round((row.count / max) * 100);
                  return (
                    <li key={row.id} className="rounded-xl bg-white/60 p-3">
                      <div className="flex items-center justify-between text-[12px] font-medium">
                        <span className="truncate">{row.name}</span>
                        <span className="text-muted-foreground">{row.count}</span>
                      </div>
                      <div className="mt-1.5 h-1.5 rounded-full bg-muted overflow-hidden">
                        <div className="h-full bg-copilot-gradient" style={{ width: `${pct}%` }} />
                      </div>
                    </li>
                  );
                })
              )}
            </ul>
          </section>
        </div>
      ) : (
        <div className="glass rounded-2xl p-10 text-center">
          <div className="mx-auto size-14 rounded-2xl bg-copilot-gradient grid place-items-center text-white shadow-md">
            <Sparkles className="size-6" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No contributions yet</h3>
          <p className="mt-1 text-sm text-muted-foreground max-w-md mx-auto">
            Capture your first contribution against an Initiative to build organizational memory Copilot can reuse.
          </p>
          <Button onClick={() => openAddContribution()} className="mt-4 rounded-xl bg-copilot-gradient text-white">
            <Plus className="size-4" /> Add Contribution
          </Button>
        </div>
      )}

      <div>
        <div className="flex items-end justify-between mb-3">
          <div>
            <h2 className="text-lg font-semibold tracking-tight">Team members</h2>
            <p className="text-sm text-muted-foreground">Initiatives Assigned, deliverables shipped, and impact score.</p>
          </div>
        </div>
        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
          {teamMembers.map((m) => (
            <div key={m.id} className="glass rounded-2xl p-5">
              <div className="flex items-start gap-3">
                <div
                  className={cn(
                    "size-12 rounded-2xl grid place-items-center text-white font-semibold shadow-md bg-gradient-to-br",
                    m.avatarColor
                  )}
                >
                  {m.name.split(" ").map((n) => n[0]).join("").slice(0, 2)}
                </div>
                <div className="flex-1">
                  <div className="font-semibold leading-tight">{m.name}</div>
                  <div className="text-xs text-muted-foreground">{m.role}</div>
                </div>
                <div className="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-white/70 border border-white/60 inline-flex items-center gap-1">
                  <Award className="size-3 text-amber-500" />
                  {m.impactScore}
                </div>
              </div>

              <div className="mt-4 grid grid-cols-3 gap-2 text-center">
                <div className="rounded-xl bg-white/60 py-2">
                  <div className="text-lg font-semibold">{m.contributions}</div>
                  <div className="text-[10px] text-muted-foreground uppercase tracking-wide">Contributions</div>
                </div>
                <div className="rounded-xl bg-white/60 py-2">
                  <div className="text-lg font-semibold">{deliverables.length}</div>
                  <div className="text-[10px] text-muted-foreground uppercase tracking-wide">Initiatives</div>
                </div>
                <div className="rounded-xl bg-white/60 py-2">
                  <div className="text-lg font-semibold">{m.impactScore}</div>
                  <div className="text-[10px] text-muted-foreground uppercase tracking-wide">Impact</div>
                </div>
              </div>

              <div className="mt-3 flex flex-wrap gap-1.5">
                {m.focus.map((f) => (
                  <span key={f} className="text-[10px] px-2 py-0.5 rounded-full bg-white/70 border border-white/60">
                    {f}
                  </span>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
