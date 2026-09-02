import { useQuery } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "./use-auth";

/** Mirrors UserResponseDto from TeamIntelligenceHub.Application. */
export type BackendUser = {
  id: number;
  entraObjectId: string;
  email: string;
  displayName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
};

export const backendUserQueryKey = ["backend-user", "me"] as const;

/**
 * Resolves the signed-in user against the backend.
 *
 * `GET /api/users/me` provisions the row on first sign-in and refreshes the profile and
 * last-login stamp afterwards, so calling this is what actually puts the user in the
 * database. The result is cached, so any component can call the hook without re-fetching.
 */
export function useBackendUser() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: backendUserQueryKey,
    queryFn: () => apiFetch<BackendUser>("/api/users/me"),
    enabled: isAuthenticated && isApiConfigured,
    staleTime: 5 * 60 * 1000,
  });
}

export default useBackendUser;
