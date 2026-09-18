import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type PropsWithChildren,
} from "react";
import { useQueries } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "@/hooks/use-auth";
import { useInitiativesQuery } from "@/hooks/use-initiatives-api";
import {
  contributionsQueryKey,
  reuseTargetToLabel,
  type ContributionRecord,
} from "@/hooks/use-contributions";
import { AddContributionWizard } from "./AddContributionWizard";

/**
 * The shape the pages read.
 *
 * Kept close to what it was when this was local state, so Team Contributions, the
 * Initiative detail header, and the Team tab did not all have to change at once. The one
 * thing to know: `initiativeId` is a string here because route params are strings, while
 * the API keys on an int.
 */
export interface Contribution {
  id: string;
  initiativeId: string;
  initiativeName: string;
  workstream: string;
  submittedBy: string;
  submissionDate: string;
  types: string[];
  title: string;
  description: string;
  keyTakeaway: string;
  priority: string;
  tags: string[];
  files: {
    id: string;
    attachmentId: number;
    contributionId: number;
    name: string;
    size: string;
    type: string;
    createdAt: string;
  }[];
  links: { id: string; source: string; url: string; label: string }[];
  contributors: { id: string; name: string; role: string; area: string; primary: boolean }[];
  visibility: string[];
  status: "draft" | "submitted";
  createdAt: string;
}

function humanSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function toContribution(record: ContributionRecord): Contribution {
  return {
    id: String(record.id),
    initiativeId: String(record.initiativeId),
    initiativeName: record.initiativeName,
    workstream: record.workstream,
    submittedBy: record.submittedByDisplayName,
    submissionDate: record.submittedAt ?? record.createdAt,
    types: record.types,
    title: record.title,
    description: record.description,
    keyTakeaway: record.keyTakeaway ?? "",
    priority: record.priority,
    tags: record.tags,
    files: record.attachments.map((a) => ({
      id: String(a.id),
      attachmentId: a.id,
      contributionId: record.id,
      name: a.fileName,
      size: humanSize(a.fileSize),
      type: a.contentType,
      createdAt: a.createdAt,
    })),
    links: record.links.map((l) => ({
      id: String(l.id),
      source: l.source,
      url: l.url,
      label: l.label ?? l.url,
    })),
    contributors: record.contributors.map((c) => ({
      id: String(c.userId),
      name: c.displayName,
      role: "",
      area: c.responsibilityArea ?? "",
      primary: c.isPrimary,
    })),
    visibility: record.reuseTargets.map(reuseTargetToLabel),
    status: record.status === "Submitted" ? "submitted" : "draft",
    createdAt: record.createdAt,
  };
}

export interface OpenAddContributionOptions {
  initiativeId?: string;
}

interface ContributionContextValue {
  contributions: Contribution[];
  isLoading: boolean;
  openAddContribution: (opts?: OpenAddContributionOptions) => void;
  openEditContribution: (contributionId: string) => void;
}

const ContributionContext = createContext<ContributionContextValue | null>(null);

export function ContributionProvider({ children }: PropsWithChildren) {
  const [open, setOpen] = useState(false);
  const [preselectedInitiativeId, setPreselectedInitiativeId] = useState<string | undefined>(undefined);
  const [editingContributionId, setEditingContributionId] = useState<string | undefined>(undefined);

  const { isAuthenticated } = useAuth();
  const { data: initiatives = [] } = useInitiativesQuery();

  /**
   * Contributions are fetched per Initiative, because that is how the API scopes them.
   *
   * Pages such as Team Contributions want them all at once, so the per-Initiative
   * queries are fanned out here and flattened. Each one caches under the same key the
   * write hooks invalidate, so a new contribution appears without a refetch loop. If the
   * Initiative count grows past a few dozen this wants one endpoint rather than a
   * fan-out.
   */
  const results = useQueries({
    queries: initiatives.map((initiative) => ({
      queryKey: contributionsQueryKey(initiative.id),
      queryFn: () =>
        apiFetch<ContributionRecord[]>(
          `/api/initiatives/${initiative.id}/contributions`,
        ),
      enabled: isAuthenticated && isApiConfigured,
    })),
  });

  // Keyed on the fetch stamps rather than the result objects, which useQueries returns
  // fresh on every render and would otherwise rebuild this list each time.
  const stamps = results.map((result) => result.dataUpdatedAt).join(",");

  const contributions = useMemo(
    () =>
      results
        .flatMap((result) => result.data ?? [])
        .map(toContribution)
        .sort((a, b) => b.createdAt.localeCompare(a.createdAt)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [stamps],
  );

  const isLoading = results.some((result) => result.isLoading);

  const openAddContribution = useCallback((opts?: OpenAddContributionOptions) => {
    setEditingContributionId(undefined);
    setPreselectedInitiativeId(opts?.initiativeId);
    setOpen(true);
  }, []);

  const openEditContribution = useCallback((contributionId: string) => {
    setPreselectedInitiativeId(undefined);
    setEditingContributionId(contributionId);
    setOpen(true);
  }, []);

  const value = useMemo(
    () => ({ contributions, isLoading, openAddContribution, openEditContribution }),
    [contributions, isLoading, openAddContribution, openEditContribution],
  );

  return (
    <ContributionContext.Provider value={value}>
      {children}
      <AddContributionWizard
        open={open}
        onOpenChange={setOpen}
        preselectedInitiativeId={preselectedInitiativeId}
        editingContributionId={editingContributionId}
      />
    </ContributionContext.Provider>
  );
}

export function useAddContribution() {
  const ctx = useContext(ContributionContext);
  if (!ctx) throw new Error("useAddContribution must be used within ContributionProvider");
  return ctx;
}
