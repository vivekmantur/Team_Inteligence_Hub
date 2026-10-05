import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { membersQueryKey } from "./use-initiative-members";
import { tasksQueryKey } from "./use-initiative-tasks";
import { useAuth } from "./use-auth";

/** Mirrors ActivityMentionDto. */
export type ActivityMention = {
  mentionedUserId: number;
  displayName: string;
};

/** Mirrors ActivityCreatedTaskDto. */
export type ActivityCreatedTask = {
  id: number;
  title: string;
  assignedToUserId: number | null;
  assignedToDisplayName: string | null;
};

/** Mirrors ActivityResponseDto. */
export type ActivityRecord = {
  id: number;
  initiativeId: number;
  userId: number;
  userDisplayName: string;
  activityMessage: string;
  autoCreateTaskEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
  mentions: ActivityMention[];
  createdTasks: ActivityCreatedTask[];
};

/** Body for creating an activity entry. */
export type CreateActivityRequest = {
  activityMessage: string;
  autoCreateTaskEnabled: boolean;
  mentionedUserIds?: number[];
};

/** Body for editing an activity entry. */
export type UpdateActivityRequest = {
  activityMessage: string;
  mentionedUserIds?: number[];
};

/** Builds the React Query cache key for one Initiative's activity feed. */
export const activityQueryKey = (initiativeId: number) =>
  ["initiatives", initiativeId, "activity"] as const;

/**
 * Loads the activity feed for one Initiative. Disabled until the user is signed in, the
 * API is configured, and an initiativeId is set.
 */
export function useActivityFeed(initiativeId: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: activityQueryKey(initiativeId ?? 0),
    queryFn: () =>
      apiFetch<ActivityRecord[]>(`/api/initiatives/${initiativeId}/activity`),
    enabled: isAuthenticated && isApiConfigured && initiativeId !== null,
  });
}

/**
 * Refreshes the feed, the task board, and the team after a write.
 *
 * Posting with auto-create raises tasks, and assigning a task enrols the assignee on the
 * Initiative — so one POST here can change all three lists.
 */
function useActivityRefresh(initiativeId: number | null) {
  const queryClient = useQueryClient();
  const id = initiativeId ?? 0;

  return () => {
    void queryClient.invalidateQueries({ queryKey: activityQueryKey(id) });
    void queryClient.invalidateQueries({ queryKey: tasksQueryKey(id) });
    void queryClient.invalidateQueries({ queryKey: membersQueryKey(id) });
  };
}

/**
 * Posts a new activity entry to the Initiative, then refreshes the feed, tasks, and team.
 */
export function useCreateActivity(initiativeId: number | null) {
  const refresh = useActivityRefresh(initiativeId);

  return useMutation({
    mutationFn: (request: CreateActivityRequest) =>
      apiFetch<ActivityRecord>(`/api/initiatives/${initiativeId}/activity`, {
        method: "POST",
        body: JSON.stringify(request),
      }),
    onSuccess: refresh,
  });
}

/**
 * Replaces an activity entry's message and mentions (PUT), then refreshes the feed, tasks,
 * and team.
 */
export function useUpdateActivity(initiativeId: number | null) {
  const refresh = useActivityRefresh(initiativeId);

  return useMutation({
    mutationFn: ({
      activityId,
      ...request
    }: UpdateActivityRequest & { activityId: number }) =>
      apiFetch<ActivityRecord>(
        `/api/initiatives/${initiativeId}/activity/${activityId}`,
        { method: "PUT", body: JSON.stringify(request) },
      ),
    onSuccess: refresh,
  });
}

/** Deletes an activity entry by id, then refreshes the feed, tasks, and team. */
export function useDeleteActivity(initiativeId: number | null) {
  const refresh = useActivityRefresh(initiativeId);

  return useMutation({
    mutationFn: (activityId: number) =>
      apiFetch<void>(`/api/initiatives/${initiativeId}/activity/${activityId}`, {
        method: "DELETE",
      }),
    onSuccess: refresh,
  });
}
