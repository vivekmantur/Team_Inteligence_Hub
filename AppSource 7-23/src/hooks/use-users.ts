import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { backendUserQueryKey, type BackendUser } from "./use-backend-user";
import { useAuth } from "./use-auth";

export const usersQueryKey = ["users"] as const;

/**
 * Everyone who has signed in at least once. These are the only people who can own or
 * sponsor an Initiative, because the Owner column is a foreign key onto this table.
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
 * Sets a user's AppRole. The API only ever accepts this for the caller's own id — this
 * hook still takes a userId so the Team page's row-level "is this me?" check and the
 * write call share the same value, rather than trusting a separate assumption.
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
