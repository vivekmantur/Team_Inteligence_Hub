import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiBaseUrl, apiFetch, getAccessToken, isApiConfigured } from "@/lib/api-client";
import { useAuth } from "./use-auth";

/** Mirrors TaskCommentAttachmentDto. */
export type CommentAttachment = {
  id: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
};

/** Mirrors TaskCommentMentionDto. */
export type CommentMention = {
  mentionedUserId: number;
  displayName: string;
};

/** Mirrors TaskCommentResponseDto. */
export type CommentRecord = {
  id: number;
  taskId: number;
  userId: number;
  userDisplayName: string;
  parentCommentId: number | null;
  commentText: string;
  createdAt: string;
  updatedAt: string | null;
  mentions: CommentMention[];
  attachments: CommentAttachment[];
};

export type SaveCommentRequest = {
  commentText: string;
  parentCommentId?: number | null;
  mentionedUserIds?: number[];
};

export const commentsQueryKey = (taskId: number) =>
  ["tasks", taskId, "comments"] as const;

export function useTaskComments(taskId: number | null) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: commentsQueryKey(taskId ?? 0),
    queryFn: () => apiFetch<CommentRecord[]>(`/api/tasks/${taskId}/comments`),
    enabled: isAuthenticated && isApiConfigured && taskId !== null,
  });
}

function useCommentsRefresh(taskId: number | null) {
  const queryClient = useQueryClient();

  return () => {
    void queryClient.invalidateQueries({ queryKey: commentsQueryKey(taskId ?? 0) });
  };
}

export function useCreateComment(taskId: number | null) {
  const refresh = useCommentsRefresh(taskId);

  return useMutation({
    mutationFn: (request: SaveCommentRequest) =>
      apiFetch<CommentRecord>(`/api/tasks/${taskId}/comments`, {
        method: "POST",
        body: JSON.stringify(request),
      }),
    onSuccess: refresh,
  });
}

export function useUpdateComment(taskId: number | null) {
  const refresh = useCommentsRefresh(taskId);

  return useMutation({
    mutationFn: ({
      commentId,
      ...request
    }: SaveCommentRequest & { commentId: number }) =>
      apiFetch<CommentRecord>(`/api/tasks/${taskId}/comments/${commentId}`, {
        method: "PUT",
        body: JSON.stringify(request),
      }),
    onSuccess: refresh,
  });
}

export function useDeleteComment(taskId: number | null) {
  const refresh = useCommentsRefresh(taskId);

  return useMutation({
    mutationFn: (commentId: number) =>
      apiFetch<void>(`/api/tasks/${taskId}/comments/${commentId}`, {
        method: "DELETE",
      }),
    onSuccess: refresh,
  });
}

/**
 * Uploads one file against a comment.
 *
 * Sent as multipart rather than through apiFetch, which sets a JSON content type. The
 * browser has to set its own boundary here, so the token is attached by hand.
 */
export function useUploadAttachment(taskId: number | null) {
  const refresh = useCommentsRefresh(taskId);

  return useMutation({
    mutationFn: async ({ commentId, file }: { commentId: number; file: File }) => {
      const token = await getAccessToken();
      const form = new FormData();
      form.append("file", file);

      const response = await fetch(
        `${apiBaseUrl}/api/tasks/${taskId}/comments/${commentId}/attachments`,
        {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
          body: form,
        },
      );

      if (!response.ok) {
        const body = await response.text();
        let message = `${response.status} ${response.statusText}`;
        try {
          const parsed = JSON.parse(body);
          if (parsed?.message) message = String(parsed.message);
        } catch {
          // Non-JSON error body — keep the status line.
        }
        throw new Error(message);
      }

      return (await response.json()) as CommentAttachment;
    },
    onSuccess: refresh,
  });
}

export function useDeleteAttachment(taskId: number | null) {
  const refresh = useCommentsRefresh(taskId);

  return useMutation({
    mutationFn: (attachmentId: number) =>
      apiFetch<void>(`/api/tasks/${taskId}/attachments/${attachmentId}`, {
        method: "DELETE",
      }),
    onSuccess: refresh,
  });
}

/**
 * Fetches an attachment and hands it to the browser as a download.
 *
 * The container is private and the endpoint requires a bearer token, so a plain link
 * cannot work — the bytes have to be fetched and turned into an object URL.
 */
export async function downloadAttachment(
  taskId: number,
  attachment: CommentAttachment,
): Promise<void> {
  const token = await getAccessToken();

  const response = await fetch(
    `${apiBaseUrl}/api/tasks/${taskId}/attachments/${attachment.id}/download`,
    { headers: { Authorization: `Bearer ${token}` } },
  );

  if (!response.ok) {
    throw new Error(`Could not download ${attachment.fileName}.`);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;
  link.download = attachment.fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();

  URL.revokeObjectURL(url);
}
