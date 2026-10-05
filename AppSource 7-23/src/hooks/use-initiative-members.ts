import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "./use-auth";

/** Mirrors InitiativeMemberResponseDto from TeamIntelligenceHub.Application. */
export type InitiativeMemberRecord = {
  id: number;
  initiativeId: number;
  userId: number;
  userDisplayName: string;
  userEmail: string;
  role: string;
  responsibilityArea: string | null;
  allocation: number | null;
  /** This person's Allocation summed across every Initiative they belong to, not just this one. */
  totalAllocationAcrossInitiatives: number;
  joinedAt: string;
};

/** Body for adding a user to an Initiative's team. */
export type AddMemberRequest = {
  userId: number;
  role: string;
  responsibilityArea?: string;
  allocation?: number;
};

/** Body for editing a team member's role, responsibility area, or allocation. */
export type UpdateMemberRequest = {
  role: string;
  responsibilityArea?: string;
  allocation?: number;
};

/** Builds the React Query cache key for one Initiative's member list. */
export const membersQueryKey = (initiativeId: number) =>
  ["initiatives", initiativeId, "members"] as const;

/**
 * Loads one Initiative's team members. Disabled until the user is signed in, the API is
 * configured, and an initiativeId is set.
 */
export function useInitiativeMembers(initiativeId: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: membersQueryKey(initiativeId ?? 0),
    queryFn: () =>
      apiFetch<InitiativeMemberRecord[]>(`/api/initiatives/${initiativeId}/members`),
    enabled: isAuthenticated && isApiConfigured && initiativeId !== null,
  });
}

/**
 * Adds a user to the Initiative's team, then refreshes the member list.
 *
 * This and the other member mutations below invalidate the member list on success, so the
 * panel shows what the server stored rather than an optimistic guess.
 */
export function useAddInitiativeMember(initiativeId: number | null) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: AddMemberRequest) =>
      apiFetch<InitiativeMemberRecord>(`/api/initiatives/${initiativeId}/members`, {
        method: "POST",
        body: JSON.stringify(request),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: membersQueryKey(initiativeId ?? 0),
      });
    },
  });
}

/** Replaces a team member's role, responsibility area, and allocation (PUT), then refreshes the member list. */
export function useUpdateInitiativeMember(initiativeId: number | null) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ memberId, ...request }: UpdateMemberRequest & { memberId: number }) =>
      apiFetch<InitiativeMemberRecord>(
        `/api/initiatives/${initiativeId}/members/${memberId}`,
        { method: "PUT", body: JSON.stringify(request) },
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: membersQueryKey(initiativeId ?? 0),
      });
    },
  });
}

/** Removes a member from the Initiative's team by member id, then refreshes the member list. */
export function useRemoveInitiativeMember(initiativeId: number | null) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (memberId: number) =>
      apiFetch<void>(`/api/initiatives/${initiativeId}/members/${memberId}`, {
        method: "DELETE",
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: membersQueryKey(initiativeId ?? 0),
      });
    },
  });
}
