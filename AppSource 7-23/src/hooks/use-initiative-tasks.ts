import { useMemo } from "react";
import { useQueries, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch, isApiConfigured } from "@/lib/api-client";
import { membersQueryKey } from "./use-initiative-members";
import { useAuth } from "./use-auth";
import { useInitiativesQuery } from "./use-initiatives-api";
import { useBackendUser } from "./use-backend-user";

/**
 * The API speaks enum names; the UI shows display labels. Translating between them is
 * this module's job so no component has to know the wire format.
 */
export type TaskStatusWire = "NotStarted" | "InProgress" | "Blocked" | "Done";
/** Task priority as the API sends it. */
export type TaskPriorityWire = "High" | "Medium" | "Low";

const TASK_STATUS_LABELS: Record<TaskStatusWire, string> = {
  NotStarted: "Not Started",
  InProgress: "In Progress",
  Blocked: "Blocked",
  Done: "Done",
};

/** Returns the display label for a task status, or the wire value if it has no label. */
export const taskStatusToLabel = (wire: TaskStatusWire): string =>
  TASK_STATUS_LABELS[wire] ?? wire;

/** Returns the wire value for a task status label, falling back to "NotStarted" when the label is unknown. */
export const taskStatusToWire =(label: string): TaskStatusWire => {
  const match = (Object.keys(TASK_STATUS_LABELS) as TaskStatusWire[]).find(
    (key) => TASK_STATUS_LABELS[key] === label,
  );
  return match ?? "NotStarted";
};

/** Mirrors TaskResponseDto from TeamIntelligenceHub.Application. */
export type TaskRecord = {
  id: number;
  initiativeId: number;
  title: string;
  assignedToUserId: number | null;
  assignedToDisplayName: string | null;
  createdByUserId: number;
  createdByDisplayName: string | null;
  dueDate: string | null;
  priority: TaskPriorityWire;
  status: TaskStatusWire;
  createdAt: string;
  updatedAt: string | null;
};

/** Body for creating or editing a task. */
export type SaveTaskRequest = {
  title: string;
  assignedToUserId?: number | null;
  dueDate?: string | null;
  priority?: TaskPriorityWire;
  status?: TaskStatusWire;
};

/** Builds the React Query cache key for one Initiative's task list. */
export const tasksQueryKey = (initiativeId: number) =>
  ["initiatives", initiativeId, "tasks"] as const;

/**
 * Loads one Initiative's tasks. Disabled until the user is signed in, the API is
 * configured, and an initiativeId is set.
 */
export function useInitiativeTasks(initiativeId: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: tasksQueryKey(initiativeId ?? 0),
    queryFn: () => apiFetch<TaskRecord[]>(`/api/initiatives/${initiativeId}/tasks`),
    enabled: isAuthenticated && isApiConfigured && initiativeId !== null,
  });
}

/**
 * How many tasks assigned to the signed-in user are not yet Done, across every
 * Initiative.
 *
 * Tasks are only readable per-Initiative, so this fans the read out the same way
 * ContributionContext does for contributions: one query per Initiative, flattened and
 * filtered client-side. This suits a few dozen Initiatives; beyond that, an aggregate
 * "my tasks" endpoint is the better fit. The per-Initiative queries wait until the
 * backend user has loaded, and the count is 0 until then.
 */
export function useMyOpenTasksCount() {
  const { isAuthenticated } = useAuth();
  const { data: initiatives = [] } = useInitiativesQuery();
  const { data: backendUser } = useBackendUser();

  const results = useQueries({
    queries: initiatives.map((initiative) => ({
      queryKey: tasksQueryKey(initiative.id),
      queryFn: () => apiFetch<TaskRecord[]>(`/api/initiatives/${initiative.id}/tasks`),
      enabled: isAuthenticated && isApiConfigured && backendUser !== undefined,
    })),
  });

  const isLoading = results.some((result) => result.isLoading);
  const stamps = results.map((result) => result.dataUpdatedAt).join(",");

  const count = useMemo(() => {
    if (!backendUser) return 0;

    return results
      .flatMap((result) => result.data ?? [])
      .filter((task) => task.assignedToUserId === backendUser.id && task.status !== "Done")
      .length;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [stamps, backendUser]);

  return { count, isLoading };
}

/**
 * Refreshes tasks and the team after a write.
 *
 * The team list matters because assigning work enrols the assignee on the Initiative,
 * so a task change can add a member as a side effect.
 */
function useTaskMutationRefresh(initiativeId: number | null) {
  const queryClient = useQueryClient();

  return () => {
    void queryClient.invalidateQueries({
      queryKey: tasksQueryKey(initiativeId ?? 0),
    });
    void queryClient.invalidateQueries({
      queryKey: membersQueryKey(initiativeId ?? 0),
    });
  };
}

/** Creates a task on the Initiative, then refreshes its tasks and team. */
export function useCreateTask(initiativeId: number | null) {
  const refresh = useTaskMutationRefresh(initiativeId);

  return useMutation({
    mutationFn: (request: SaveTaskRequest) =>
      apiFetch<TaskRecord>(`/api/initiatives/${initiativeId}/tasks`, {
        method: "POST",
        body: JSON.stringify(request),
      }),
    onSuccess: refresh,
  });
}

/** Replaces a task's fields (PUT), then refreshes the Initiative's tasks and team. */
export function useUpdateTask(initiativeId: number | null) {
  const refresh = useTaskMutationRefresh(initiativeId);

  return useMutation({
    mutationFn: ({ taskId, ...request }: SaveTaskRequest & { taskId: number }) =>
      apiFetch<TaskRecord>(`/api/initiatives/${initiativeId}/tasks/${taskId}`, {
        method: "PUT",
        body: JSON.stringify(request),
      }),
    onSuccess: refresh,
  });
}

/** Deletes a task by id, then refreshes the Initiative's tasks and team. */
export function useDeleteTask(initiativeId: number | null) {
  const refresh = useTaskMutationRefresh(initiativeId);

  return useMutation({
    mutationFn: (taskId: number) =>
      apiFetch<void>(`/api/initiatives/${initiativeId}/tasks/${taskId}`, {
        method: "DELETE",
      }),
    onSuccess: refresh,
  });
}
