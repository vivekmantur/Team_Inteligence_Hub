// In-memory mock data layer for the Team Intelligence Hub.
// NOTE(ai): Everything is ephemeral. Refresh = reset.

// Initiative status (previously ProjectStatus — alias kept for backward compatibility)
export type InitiativeStatus = "On Track" | "At Risk" | "Completed" | "Planning";
export type ProjectStatus = InitiativeStatus;

export interface Initiative {
  id: string;
  name: string;
  workstream: string;
  status: InitiativeStatus;
  progress: number;
  owner: string;
  updated: string;
  description: string;
  tags: string[];
}
// Backward-compat alias
export type Project = Initiative;

export interface Deliverable {
  id: string;
  initiativeId: string;
  /** @deprecated use initiativeId */
  projectId?: string;
  title: string;
  type: "Playbook" | "Workshop" | "Dashboard" | "Campaign" | "Enablement";
  status: "Draft" | "In Review" | "Shipped";
  due: string;
}

export interface Metric {
  id: string;
  name: string;
  value: number;
  unit: string;
  trend: number; // % change
  category: "Adoption" | "Engagement" | "Sentiment" | "Impact";
}

export interface TeamMember {
  id: string;
  name: string;
  role: string;
  contributions: number;
  impactScore: number;
  focus: string[];
  avatarColor: string;
}
export type InitiativeMember = TeamMember;

export interface CustomerZeroStory {
  id: string;
  title: string;
  problem: string;
  solution: string;
  impact: string;
  metric: string;
  quote: string;
  quoteAuthor: string;
  vertical: string;
  status: "Published" | "Draft" | "In Review";
}

export interface Testimonial {
  id: string;
  author: string;
  role: string;
  audience: "Leadership" | "Stakeholder" | "Customer" | "Team";
  quote: string;
  sentiment: "Positive" | "Neutral" | "Constructive";
  date: string;
}

export interface FeedbackItem {
  id: string;
  source: "Listening Session" | "Huddle" | "Survey" | "Feedback Loop";
  theme: string;
  sentiment: number; // -1 to 1
  volume: number;
  quote: string;
  date: string;
}

export interface ContentAsset {
  id: string;
  name: string;
  type: "PPT" | "PDF" | "Word" | "Excel" | "Image" | "Video";
  size: string;
  owner: string;
  updated: string;
  tags: string[];
}

export interface RoleHubAnalytic {
  role: string;
  activeUsers: number;
  sessions: number;
  adoption: number; // %
  satisfaction: number; // 0-100
}

export interface AgentAnalytic {
  agent: string;
  invocations: number;
  successRate: number;
  avgLatencyMs: number;
  savedHours: number;
}

export interface GeneratedContent {
  id: string;
  type: "LinkedIn Post" | "Newsletter" | "Executive Summary" | "QBR Slide" | "Blog" | "Case Study" | "Viva Engage Post";
  title: string;
  tone: string;
  audience: string;
  length: string;
  createdAt: string;
  preview: string;
}

export const initiatives: Initiative[] = [
  { id: "p1", name: "Role Hub Global Rollout", workstream: "Adoption", status: "On Track", progress: 78, owner: "Priya Menon", updated: "2 days ago", description: "Scaling Role Hub across 14 regions with localized enablement.", tags: ["Rollout", "Enablement", "Global"] },
  { id: "p2", name: "Copilot Adoption Playbook v3", workstream: "Change Management", status: "On Track", progress: 62, owner: "Marcus Chen", updated: "1 day ago", description: "Next-gen adoption playbook aligned with FY26 GTM.", tags: ["Playbook", "Copilot"] },
  { id: "p3", name: "Customer Zero Program", workstream: "Storytelling", status: "On Track", progress: 84, owner: "Amara Okafor", updated: "5 hours ago", description: "Curated portfolio of internal proof points.", tags: ["Storytelling", "Evidence"] },
  { id: "p4", name: "Sentiment Listening Engine", workstream: "Insights", status: "At Risk", progress: 41, owner: "Ravi Iyer", updated: "3 days ago", description: "Unified pipeline for huddles, surveys, and feedback loops.", tags: ["Analytics", "AI"] },
  { id: "p5", name: "Executive Narrative Studio", workstream: "Marketing", status: "Planning", progress: 18, owner: "Elena Rossi", updated: "6 days ago", description: "Content studio for leadership-ready briefs and decks.", tags: ["Marketing", "Executive"] },
  { id: "p6", name: "Agent Analytics Fabric", workstream: "Insights", status: "Completed", progress: 100, owner: "Diego Alvarez", updated: "2 weeks ago", description: "Real-time analytics for internal agent portfolio.", tags: ["Agents", "Fabric"] },
];
// Backward-compat alias so any legacy imports keep working.
export const projects = initiatives;

export const deliverables: Deliverable[] = [
  { id: "d1", initiativeId: "p1", projectId: "p1", title: "Region APAC enablement kit", type: "Enablement", status: "Shipped", due: "Jun 22" },
  { id: "d2", initiativeId: "p1", projectId: "p1", title: "EMEA localization pack", type: "Playbook", status: "In Review", due: "Jul 08" },
  { id: "d3", initiativeId: "p2", projectId: "p2", title: "Change champion workshop", type: "Workshop", status: "Shipped", due: "Jun 12" },
  { id: "d4", initiativeId: "p3", projectId: "p3", title: "Copilot-in-Sales case study", type: "Campaign", status: "Shipped", due: "Jun 30" },
  { id: "d5", initiativeId: "p4", projectId: "p4", title: "Sentiment dashboard v1", type: "Dashboard", status: "Draft", due: "Jul 15" },
  { id: "d6", initiativeId: "p5", projectId: "p5", title: "CEO leadership brief", type: "Campaign", status: "Draft", due: "Jul 20" },
];

export const metrics: Metric[] = [
  { id: "m1", name: "Monthly Active Users", value: 128400, unit: "users", trend: 18.2, category: "Adoption" },
  { id: "m2", name: "Role Hub Sessions", value: 42210, unit: "sessions", trend: 26.1, category: "Engagement" },
  { id: "m3", name: "Copilot Prompts", value: 984320, unit: "prompts", trend: 34.7, category: "Engagement" },
  { id: "m4", name: "Sentiment Score", value: 78, unit: "/100", trend: 4.2, category: "Sentiment" },
  { id: "m5", name: "Hours Saved (Q)", value: 12480, unit: "hrs", trend: 22.9, category: "Impact" },
  { id: "m6", name: "NPS", value: 62, unit: "nps", trend: 8.5, category: "Sentiment" },
];

export const teamMembers: TeamMember[] = [
  { id: "t1", name: "Priya Menon", role: "Adoption Lead", contributions: 42, impactScore: 94, focus: ["Role Hub", "Enablement"], avatarColor: "from-indigo-500 to-fuchsia-500" },
  { id: "t2", name: "Marcus Chen", role: "Change Strategist", contributions: 38, impactScore: 91, focus: ["Playbooks", "GTM"], avatarColor: "from-sky-500 to-indigo-500" },
  { id: "t3", name: "Amara Okafor", role: "Storytelling Lead", contributions: 55, impactScore: 96, focus: ["Customer Zero", "Narrative"], avatarColor: "from-rose-500 to-orange-500" },
  { id: "t4", name: "Ravi Iyer", role: "Insights Architect", contributions: 31, impactScore: 88, focus: ["Analytics", "AI"], avatarColor: "from-emerald-500 to-teal-500" },
  { id: "t5", name: "Elena Rossi", role: "Executive Comms", contributions: 27, impactScore: 90, focus: ["Executive", "Marketing"], avatarColor: "from-fuchsia-500 to-purple-500" },
  { id: "t6", name: "Diego Alvarez", role: "Agent PM", contributions: 33, impactScore: 89, focus: ["Agents", "Fabric"], avatarColor: "from-amber-500 to-rose-500" },
];

export const customerZero: CustomerZeroStory[] = [
  { id: "cz1", title: "Role Hub cuts onboarding time by 43%", problem: "New hires spent 6+ weeks discovering role-relevant assets.", solution: "Deployed Role Hub with AI-curated learning journeys.", impact: "43% faster onboarding, 92% satisfaction.", metric: "-43% time to productivity", quote: "Role Hub is the single pane of glass we needed.", quoteAuthor: "CVP, Modern Work", vertical: "Internal", status: "Published" },
  { id: "cz2", title: "Copilot in Field: 12,480 hours reclaimed", problem: "Field sellers drowning in repetitive prep work.", solution: "Rolled out tailored Copilot prompts and templates.", impact: "12,480 hours saved this quarter.", metric: "+22.9% productivity", quote: "I'm getting Fridays back.", quoteAuthor: "Senior Account Exec", vertical: "Sales", status: "Published" },
  { id: "cz3", title: "Agent Fabric powers real-time insights", problem: "Analytics fragmented across 9 sources.", solution: "Unified Fabric layer with agent-driven summarization.", impact: "5x faster executive readouts.", metric: "-80% report cycle time", quote: "We now brief the CEO in minutes.", quoteAuthor: "Director, BizOps", vertical: "Operations", status: "In Review" },
  { id: "cz4", title: "Sentiment engine surfaces friction early", problem: "Adoption blockers only visible in quarterly surveys.", solution: "Continuous listening across huddles and feedback loops.", impact: "3 major friction points resolved in-quarter.", metric: "+8.5 NPS", quote: "We fix issues before they become escalations.", quoteAuthor: "Adoption PM", vertical: "Internal", status: "Draft" },
];

export const testimonials: Testimonial[] = [
  { id: "ts1", author: "Satya-style CVP", role: "Corporate VP", audience: "Leadership", quote: "This team is the storytelling engine of Modern Work.", sentiment: "Positive", date: "Jun 24" },
  { id: "ts2", author: "Anita Rao", role: "Director, GTM", audience: "Stakeholder", quote: "The QBR narrative was executive-ready on day one.", sentiment: "Positive", date: "Jun 20" },
  { id: "ts3", author: "Field Seller Council", role: "Customer voice", audience: "Customer", quote: "Role Hub finally made Copilot feel personal.", sentiment: "Positive", date: "Jun 18" },
  { id: "ts4", author: "Kai Nakamura", role: "Program Manager", audience: "Team", quote: "Cross-functional syncs have never been this crisp.", sentiment: "Positive", date: "Jun 15" },
  { id: "ts5", author: "Reece Patterson", role: "Analyst", audience: "Stakeholder", quote: "Would love more granular agent telemetry in Fabric.", sentiment: "Constructive", date: "Jun 11" },
];

export const feedback: FeedbackItem[] = [
  { id: "f1", source: "Listening Session", theme: "Role Hub personalization", sentiment: 0.82, volume: 128, quote: "Feels tailor-made for my role.", date: "Jun 22" },
  { id: "f2", source: "Huddle", theme: "Change velocity", sentiment: 0.35, volume: 74, quote: "Rollout pace is ambitious but manageable.", date: "Jun 19" },
  { id: "f3", source: "Survey", theme: "Copilot trust", sentiment: 0.68, volume: 210, quote: "Grounded answers build confidence.", date: "Jun 15" },
  { id: "f4", source: "Feedback Loop", theme: "Agent latency", sentiment: -0.22, volume: 41, quote: "Sometimes agents feel slow at peak hours.", date: "Jun 12" },
  { id: "f5", source: "Listening Session", theme: "Narrative clarity", sentiment: 0.74, volume: 96, quote: "Executive briefs read like a product.", date: "Jun 09" },
];

export const assets: ContentAsset[] = [
  { id: "a1", name: "FY26 QBR Deck", type: "PPT", size: "18.4 MB", owner: "Elena Rossi", updated: "1 day ago", tags: ["QBR", "Executive"] },
  { id: "a2", name: "Role Hub Adoption Report", type: "PDF", size: "4.2 MB", owner: "Priya Menon", updated: "3 days ago", tags: ["Adoption", "Report"] },
  { id: "a3", name: "Change Playbook v3", type: "Word", size: "1.8 MB", owner: "Marcus Chen", updated: "1 week ago", tags: ["Playbook"] },
  { id: "a4", name: "Sentiment metrics workbook", type: "Excel", size: "3.1 MB", owner: "Ravi Iyer", updated: "2 days ago", tags: ["Analytics"] },
  { id: "a5", name: "Customer Zero hero image", type: "Image", size: "2.7 MB", owner: "Amara Okafor", updated: "4 days ago", tags: ["Story"] },
  { id: "a6", name: "Copilot in Field walkthrough", type: "Video", size: "124 MB", owner: "Diego Alvarez", updated: "6 days ago", tags: ["Demo"] },
  { id: "a7", name: "Agent analytics one-pager", type: "PDF", size: "1.1 MB", owner: "Diego Alvarez", updated: "5 days ago", tags: ["Agents"] },
  { id: "a8", name: "Executive talking points", type: "Word", size: "0.6 MB", owner: "Elena Rossi", updated: "today", tags: ["Executive"] },
];

export const roleHub: RoleHubAnalytic[] = [
  { role: "Seller", activeUsers: 18400, sessions: 62100, adoption: 82, satisfaction: 87 },
  { role: "Marketer", activeUsers: 8600, sessions: 24100, adoption: 74, satisfaction: 84 },
  { role: "Engineer", activeUsers: 21200, sessions: 71300, adoption: 78, satisfaction: 82 },
  { role: "Support", activeUsers: 6900, sessions: 18400, adoption: 71, satisfaction: 80 },
  { role: "Executive", activeUsers: 1240, sessions: 4100, adoption: 68, satisfaction: 91 },
];

export const agents: AgentAnalytic[] = [
  { agent: "QBR Composer", invocations: 3420, successRate: 96, avgLatencyMs: 820, savedHours: 1240 },
  { agent: "Narrative Studio", invocations: 2810, successRate: 94, avgLatencyMs: 940, savedHours: 980 },
  { agent: "Sentiment Miner", invocations: 5120, successRate: 92, avgLatencyMs: 640, savedHours: 1580 },
  { agent: "Case Study Builder", invocations: 1620, successRate: 97, avgLatencyMs: 1120, savedHours: 720 },
  { agent: "Content Coach", invocations: 4180, successRate: 93, avgLatencyMs: 720, savedHours: 1140 },
];

export const initialGeneratedContent: GeneratedContent[] = [
  { id: "g1", type: "QBR Slide", title: "FY26 Q3 Executive Wins", tone: "Confident", audience: "Leadership", length: "Medium", createdAt: "2 hours ago", preview: "Team delivered 12,480 hours saved, 34% Copilot growth, and 3 Customer Zero proof points." },
  { id: "g2", type: "LinkedIn Post", title: "Role Hub adoption milestone", tone: "Inspirational", audience: "External", length: "Short", createdAt: "yesterday", preview: "We just crossed 128K monthly active users on Role Hub. Here's what personalization at scale looks like ✨" },
  { id: "g3", type: "Case Study", title: "Copilot in Field: reclaiming Fridays", tone: "Story-driven", audience: "Customers", length: "Long", createdAt: "2 days ago", preview: "Meet the field team that turned repetitive prep into strategic time — with Copilot at the center." },
];

export const kpiSnapshot = {
  totalInitiatives: initiatives.length,
  activeInitiatives: initiatives.filter((p) => p.status !== "Completed").length,
  customerZeroStories: customerZero.length,
  /** Percentage of allocated team capacity currently in use across active Initiatives. */
  teamCapacity: 82,
  /** Leadership-bound reports (QBRs, briefs) scheduled but not yet generated. */
  upcomingLeadershipReports: 3,
  /** Open risks logged against Initiatives, awaiting mitigation. */
  openRisks: initiatives.filter((p) => p.status === "At Risk").length,
  // Backward compat
  totalProjects: initiatives.length,
};

// ---------------------------------------------------------------------------
// "My Attention" — the personalized home page.
//
// A separate mock dataset rather than reshaping `initiatives`/`Initiative`: this view's
// fields (phase, impacted roles, a person tagged in an activity feed) are specific to
// "what needs this signed-in person's attention", not the general Initiative record every
// other page reads. Keeping it separate means this page's mock shape can evolve — or be
// swapped for a real "my attention" endpoint later — without touching the shared type.
// ---------------------------------------------------------------------------

export interface AttentionStat {
  id: string;
  label: string;
  value: string;
  caption: string;
}

export const attentionStats: AttentionStat[] = [
  { id: "capacity", label: "Team Capacity", value: "150%", caption: "Your mock allocation across active initiatives" },
  { id: "reports", label: "Upcoming Leadership Reports", value: "3", caption: "Mock reporting requests currently tracked" },
  { id: "risks", label: "Risks / Attention", value: "2", caption: "Initiatives needing attention or at risk" },
  { id: "actions", label: "My Open Actions", value: "4", caption: "Tasks assigned to you" },
];

export type WeeklySignalSeverity = "risk" | "review" | "checkpoint" | "new";

export interface WeeklySignal {
  id: string;
  severity: WeeklySignalSeverity;
  title: string;
  description: string;
  when: string;
}

export const thisWeekSignals: WeeklySignal[] = [
  { id: "w1", severity: "risk", title: "Sentiment Listening Engine is At Risk", description: "Review data-source coverage and unblock the next activation milestone.", when: "Today" },
  { id: "w2", severity: "review", title: "Copilot Adoption Playbook needs your review", description: "Review the role guidance before content moves into activation.", when: "Aug 26" },
  { id: "w3", severity: "checkpoint", title: "Executive Narrative Studio pilot checkpoint", description: "Confirm pilot scope and expected output for leadership reviewers.", when: "Aug 28" },
  { id: "w4", severity: "new", title: "New evidence added to Customer Zero Evidence Program", description: "A measurable adoption outcome is ready for review and reuse.", when: "New" },
];

export interface MyInitiativeSummary {
  id: string;
  name: string;
  phase: string;
  status: "On Track" | "At Risk" | "Needs Attention";
  impactedRoles: number;
  progress: number;
}

export const myInitiatives: MyInitiativeSummary[] = [
  { id: "mi1", name: "Role Hub Global Rollout", phase: "Activate", status: "On Track", impactedRoles: 5, progress: 78 },
  { id: "mi2", name: "Copilot Adoption Playbook v3", phase: "Plan", status: "Needs Attention", impactedRoles: 4, progress: 62 },
  { id: "mi3", name: "Customer Zero Evidence Program", phase: "Measure", status: "On Track", impactedRoles: 3, progress: 84 },
  { id: "mi4", name: "Sentiment Listening Engine", phase: "Activate", status: "At Risk", impactedRoles: 5, progress: 41 },
  { id: "mi5", name: "Executive Narrative Studio", phase: "Assess", status: "On Track", impactedRoles: 2, progress: 18 },
];

export interface RecentActivityItem {
  id: string;
  category: string;
  title: string;
  context: string;
}

export const recentActivity: RecentActivityItem[] = [
  { id: "ra1", category: "Risk · Progress Update", title: "Data-source coverage needs attention", context: "Sentiment Listening Engine · Nihar Pulluri" },
  { id: "ra2", category: "Customer Story · Business Metric", title: "Role-based enablement evidence captured", context: "Customer Zero Evidence Program · Amara Okafor" },
  { id: "ra3", category: "Deliverable", title: "Enterprise role guidance draft", context: "Copilot Adoption Playbook v3 · Marcus Chen" },
];

export const attentionHighlights = {
  onTrackInitiatives: 4,
  highImpactChanges: 2,
  submittedEvidence: 2,
};

export interface UpcomingDeadline {
  id: string;
  title: string;
  type: string;
  date: string;
}

export const upcomingDeadlines: UpcomingDeadline[] = [
  { id: "ud1", title: "Review Enterprise role guidance", type: "Initiative action", date: "2026-08-26" },
  { id: "ud2", title: "Approve playbook structure", type: "Initiative action", date: "2026-08-27" },
  { id: "ud3", title: "Resolve data-source coverage gap", type: "Initiative action", date: "2026-08-25" },
  { id: "ud4", title: "August Leadership Update", type: "Leadership Update", date: "Aug 28" },
  { id: "ud5", title: "September MBR Input", type: "MBR", date: "Sep 4" },
];

// ---------------------------------------------------------------------------
// "Insights" — the consolidated Adoption / Readiness / Sentiment / Audience /
// AI & Agents / Impact & Outcomes / OKRs view. Most sections reuse the metric,
// feedback, roleHub, agent, and initiative data already defined above; this
// block only adds the shapes those existing arrays don't cover.
// ---------------------------------------------------------------------------

export const insightsReadiness = {
  activeInitiatives: initiatives.filter((p) => p.status !== "Completed").length,
  atRisk: initiatives.filter((p) => p.status === "At Risk").length,
  /** Flagged separately from "At Risk" — needs a decision or unblock, not yet in trouble. */
  needsAttention: 1,
};

export interface EnterpriseRoleCoverage {
  code: string;
  name: string;
  initiativeCount: number;
  highImpactCount: number;
}

export const enterpriseRoleCoverage: EnterpriseRoleCoverage[] = [
  { code: "AE", name: "Account Executive", initiativeCount: 4, highImpactCount: 2 },
  { code: "ATS", name: "Account Technology Strategist", initiativeCount: 4, highImpactCount: 2 },
  { code: "SSP", name: "Solution Sales Professional", initiativeCount: 3, highImpactCount: 1 },
  { code: "SE", name: "Solution Engineer", initiativeCount: 3, highImpactCount: 1 },
  { code: "CE", name: "Commercial Executive", initiativeCount: 1, highImpactCount: 0 },
  { code: "CSA", name: "Cloud Solution Architect", initiativeCount: 4, highImpactCount: 2 },
  { code: "CSAM", name: "Customer Success Account Manager", initiativeCount: 3, highImpactCount: 2 },
];

export interface InitiativeOutcome {
  id: string;
  name: string;
  outcome: string;
  progress: number;
}

export const initiativeOutcomes: InitiativeOutcome[] = [
  { id: "p1", name: "Role Hub Global Rollout", outcome: "Enterprise roles can find relevant guidance faster and apply AI-supported workflows consistently.", progress: 78 },
  { id: "p2", name: "Copilot Adoption Playbook v3", outcome: "Teams reuse consistent adoption patterns instead of recreating guidance locally.", progress: 62 },
  { id: "p3", name: "Customer Zero Evidence Program", outcome: "Leadership and field teams can quickly reuse credible examples and measurable outcomes.", progress: 84 },
  { id: "p4", name: "Sentiment Listening Engine", outcome: "Program owners can act on role-level sentiment and readiness signals in-quarter.", progress: 41 },
  { id: "p5", name: "Executive Narrative Studio", outcome: "Executive summaries can be generated from grounded initiative evidence.", progress: 18 },
  { id: "p6", name: "Agent Analytics Fabric", outcome: "Teams can compare adoption and impact across the agent portfolio.", progress: 100 },
];

export interface OkrKeyResult {
  id: string;
  description: string;
  progress: number;
}

export interface Okr {
  id: string;
  objective: string;
  owner: string;
  assignedBy: string;
  quarter: string;
  keyResults: OkrKeyResult[];
}

/**
 * Placeholder OKRs — mock content only, standing in until the team's actual
 * OKRs (assigned by Jeana J.) are wired up from a real source.
 */
export const teamOkrs: Okr[] = [
  {
    id: "okr1",
    objective: "Scale Copilot adoption across every enterprise role",
    owner: "Priya Menon",
    assignedBy: "Jeana J.",
    quarter: "FY26 Q3",
    keyResults: [
      { id: "kr1", description: "Reach 150K Role Hub monthly active users", progress: 86 },
      { id: "kr2", description: "Cover all 7 configured enterprise roles with tailored guidance", progress: 100 },
      { id: "kr3", description: "Lift average role adoption above 80%", progress: 91 },
    ],
  },
  {
    id: "okr2",
    objective: "Make leadership reporting fully self-serve",
    owner: "Elena Rossi",
    assignedBy: "Jeana J.",
    quarter: "FY26 Q3",
    keyResults: [
      { id: "kr4", description: "Ship the Executive Narrative Studio to pilot", progress: 45 },
      { id: "kr5", description: "Cut QBR prep time by 50%", progress: 60 },
      { id: "kr6", description: "Publish 4 leadership-ready reports this quarter", progress: 75 },
    ],
  },
  {
    id: "okr3",
    objective: "Turn sentiment signals into proactive action",
    owner: "Ravi Iyer",
    assignedBy: "Jeana J.",
    quarter: "FY26 Q3",
    keyResults: [
      { id: "kr7", description: "Resolve data-source coverage gaps in the Sentiment Listening Engine", progress: 35 },
      { id: "kr8", description: "Reach +8 NPS improvement quarter-over-quarter", progress: 68 },
      { id: "kr9", description: "Action 90% of constructive feedback themes within 2 weeks", progress: 52 },
    ],
  },
];
