import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "./use-auth";
import { useBackendUser } from "./use-backend-user";
import { useInitiativesQuery, type ChangeImpactWire, type HealthWire, type LifecycleStageWire } from "./use-initiatives-api";
import { membersQueryKey, type InitiativeMemberRecord } from "./use-initiative-members";
import { tasksQueryKey, type TaskRecord } from "./use-initiative-tasks";

export type MyInitiativeSummary = {
  id: number;
  name: string;
  lifecycleStage: LifecycleStageWire;
  health: HealthWire;
  changeImpact: ChangeImpactWire;
  impactedRolesCount: number;
  /**
   * Share of this Initiative's tasks marked Done, 0-100. Null when it has no tasks yet
   * — there is no "progress" column on Initiative itself, so task completion is the
   * closest genuine measure of how far along the work actually is, rather than a
   * percentage invented from LifecycleStage.
   */
  progressPercent: number | null;
};

const HEALTH_RANK: Record<HealthWire, number> = {
  AtRisk: 0,
  NeedsAttention: 1,
  OnTrack: 2,
};

/**
 * Initiatives the signed-in user owns or is a team member of, newest concern first
 * (At Risk, then Needs Attention, then On Track).
 *
 * Two fan-outs, same reasoning as ContributionContext and useMyOpenTasksCount: neither
 * "who's on this Initiative" nor "this Initiative's tasks" has a company-wide endpoint,
 * so each Initiative is queried individually and the results are filtered/derived
 * client-side. The second fan-out only runs against the (small) set of Initiatives that
 * survive the first, not every Initiative in the company.
 */
export function useMyInitiatives() {
  const { isAuthenticated } = useAuth();
  const { data: initiatives = [], isLoading: isLoadingInitiatives } = useInitiativesQuery();
  const { data: backendUser } = useBackendUser();

  const memberResults = useQueries({
    queries: initiatives.map((initiative) => ({
      queryKey: membersQueryKey(initiative.id),
      queryFn: () =>
        apiFetch<InitiativeMemberRecord[]>(`/api/initiatives/${initiative.id}/members`),
      enabled: isAuthenticated && isApiConfigured && backendUser !== undefined,
    })),
  });

  const memberStamps = memberResults.map((r) => r.dataUpdatedAt).join(",");

  const myInitiatives = useMemo(() => {
    if (!backendUser) return [];

    return initiatives.filter((initiative, index) => {
      if (initiative.ownerUserId === backendUser.id) return true;

      const members = memberResults[index]?.data ?? [];
      return members.some((m) => m.userId === backendUser.id);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initiatives, backendUser, memberStamps]);

  const taskResults = useQueries({
    queries: myInitiatives.map((initiative) => ({
      queryKey: tasksQueryKey(initiative.id),
      queryFn: () => apiFetch<TaskRecord[]>(`/api/initiatives/${initiative.id}/tasks`),
      enabled: isAuthenticated && isApiConfigured,
    })),
  });

  const taskStamps = taskResults.map((r) => r.dataUpdatedAt).join(",");

  const data = useMemo<MyInitiativeSummary[]>(() => {
    const summaries = myInitiatives.map((initiative, index) => {
      const tasks = taskResults[index]?.data ?? [];
      const progressPercent =
        tasks.length === 0
          ? null
          : Math.round(
              (tasks.filter((t) => t.status === "Done").length / tasks.length) * 100,
            );

      return {
        id: initiative.id,
        name: initiative.name,
        lifecycleStage: initiative.lifecycleStage,
        health: initiative.health,
        changeImpact: initiative.changeImpact,
        impactedRolesCount: initiative.impactedRoles.length,
        progressPercent,
      };
    });

    return summaries.sort((a, b) => {
      const rankDiff = HEALTH_RANK[a.health] - HEALTH_RANK[b.health];
      return rankDiff !== 0 ? rankDiff : a.name.localeCompare(b.name);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [myInitiatives, taskStamps]);

  const isLoading =
    isLoadingInitiatives
    || memberResults.some((r) => r.isLoading)
    || taskResults.some((r) => r.isLoading);

  return { data, isLoading };
}
