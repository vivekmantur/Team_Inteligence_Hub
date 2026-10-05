import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { backendUserQueryKey, type BackendUser } from "./use-backend-user";
import { useAuth } from "./use-auth";

/** React Query cache key for the full user list. */
export const usersQueryKey = ["users"] as const;

/**
 * Everyone who has signed in at least once. These are the only people who can own or
 * sponsor an Initiative, because the Owner column is a foreign key onto this table.
 * Disabled until the user is signed in and the API is configured.
 */
export function useUsers() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: usersQueryKey,
    queryFn: () => apiFetch<BackendUser[]>("/api/users"),
    enabled: isAuthenticated && isApiConfigured,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Sets a user's AppRole, then refreshes the user list and the signed-in user's record.
 *
 * The API accepts this only for the caller's own id. The hook still takes a userId so the
 * Team page's "is this me?" check and the write use the same value.
 */
export function useUpdateAppRole() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, appRole }: { userId: number; appRole: string }) =>
      apiFetch<BackendUser>(`/api/users/${userId}/app-role`, {
        method: "PUT",
        body: JSON.stringify({ appRole }),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: usersQueryKey });
      void queryClient.invalidateQueries({ queryKey: backendUserQueryKey });
    },
  });
}
