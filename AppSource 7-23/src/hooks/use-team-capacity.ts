import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "./use-auth";
import { useUsers } from "./use-users";
import { useInitiativesQuery } from "./use-initiatives-api";
import { membersQueryKey, type InitiativeMemberRecord } from "./use-initiative-members";

export type TeamCapacityRow = {
  userId: number;
  displayName: string;
  appRole: string;
  /** This person's Allocation summed across every Initiative they belong to. */
  totalAllocationAcrossInitiatives: number;
  /** How many Initiatives they own or are a member of. */
  initiativeCount: number;
};

/**
 * Every active user's total Allocation and how many Initiatives they're on, for the
 * Team page's "Team Capacity" panel.
 *
 * Allocation itself comes straight off useUsers() — the backend already sums it per user.
 * The Initiative count doesn't have a ready-made source, so it's derived the same way
 * useMyInitiatives derives "my" Initiatives, just for every user at once: fan out each
 * Initiative's member list, then count how many Initiatives each user owns or appears in.
 */
export function useTeamCapacity() {
  const { isAuthenticated } = useAuth();
  const { data: users = [], isLoading: isLoadingUsers } = useUsers();
  const { data: initiatives = [], isLoading: isLoadingInitiatives } = useInitiativesQuery();

  const memberResults = useQueries({
    queries: initiatives.map((initiative) => ({
      queryKey: membersQueryKey(initiative.id),
      queryFn: () =>
        apiFetch<InitiativeMemberRecord[]>(`/api/initiatives/${initiative.id}/members`),
      enabled: isAuthenticated && isApiConfigured,
    })),
  });

  const memberStamps = memberResults.map((r) => r.dataUpdatedAt).join(",");

  const data = useMemo<TeamCapacityRow[]>(() => {
    const rows = users
      .filter((user) => user.isActive)
      .map((user) => {
        const initiativeCount = initiatives.filter((initiative, index) => {
          if (initiative.ownerUserId === user.id) return true;

          const members = memberResults[index]?.data ?? [];
          return members.some((m) => m.userId === user.id);
        }).length;

        return {
          userId: user.id,
          displayName: user.displayName,
          appRole: user.appRole,
          totalAllocationAcrossInitiatives: user.totalAllocationAcrossInitiatives,
          initiativeCount,
        };
      });

    return rows.sort(
      (a, b) => b.totalAllocationAcrossInitiatives - a.totalAllocationAcrossInitiatives,
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [users, initiatives, memberStamps]);

  const isLoading =
    isLoadingUsers || isLoadingInitiatives || memberResults.some((r) => r.isLoading);

  return { data, isLoading };
}
