import { useQuery } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import type { BackendUser } from "./use-backend-user";
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
