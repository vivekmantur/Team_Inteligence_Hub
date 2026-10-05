import { useQuery } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "./use-auth";

/** Mirrors UserResponseDto from TeamIntelligenceHub.Application. */
export type BackendUser = {
  id: number;
  entraObjectId: string;
  email: string;
  displayName: string;
  /** Free-text role/title this person set for themselves, shown on the Team page. */
  appRole: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  /** This person's Allocation summed across every Initiative they belong to. */
  totalAllocationAcrossInitiatives: number;
};

/** React Query cache key for the signed-in user's backend record. */
export const backendUserQueryKey = ["backend-user", "me"] as const;

/**
 * Resolves the signed-in user against the backend.
 *
 * `GET /api/users/me` provisions the row on first sign-in and refreshes the profile and
 * last-login stamp afterwards, so calling this is what actually puts the user in the
 * database. The result stays fresh for five minutes, so any component can call the hook
 * without re-fetching. Disabled until the user is signed in and the API is configured.
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
