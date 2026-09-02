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
  joinedAt: string;
};

export type AddMemberRequest = {
  userId: number;
  role: string;
  responsibilityArea?: string;
  allocation?: number;
};

export type UpdateMemberRequest = {
  role: string;
  responsibilityArea?: string;
  allocation?: number;
};

export const membersQueryKey = (initiativeId: number) =>
  ["initiatives", initiativeId, "members"] as const;

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
 * Mutations for one Initiative's team. Each invalidates the member list so the panel
 * reflects what was actually stored rather than what was optimistically assumed.
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
