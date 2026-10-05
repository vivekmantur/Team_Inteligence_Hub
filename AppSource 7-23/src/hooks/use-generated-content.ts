import { useCallback, useSyncExternalStore } from "react";
import { memory } from "@/lib/memory-store";
import { initialGeneratedContent, type GeneratedContent } from "@/data/mock";

const KEY = "generated-content";
const listeners = new Set<() => void>();

function ensure(): GeneratedContent[] {
  return memory.ensure<GeneratedContent[]>(KEY, () => [...initialGeneratedContent]);
}

function emit() {
  listeners.forEach((l) => l());
}

/**
 * Subscribes to the in-memory list of generated content, seeded from mock data, and
 * returns it with an `add` function that prepends a new item and notifies subscribers.
 * Nothing is persisted, so the list resets on page refresh.
 */
export function useGeneratedContent() {
  const subscribe = useCallback((cb: () => void) => {
    listeners.add(cb);
    return () => listeners.delete(cb);
  }, []);
  const getSnapshot = useCallback(() => ensure(), []);
  const items = useSyncExternalStore(subscribe, getSnapshot, getSnapshot);

  const add = useCallback((item: Omit<GeneratedContent, "id" | "createdAt"> & { createdAt?: string }) => {
    const list = ensure();
    const next: GeneratedContent = {
      id: crypto.randomUUID(),
      createdAt: item.createdAt ?? "just now",
      ...item,
    };
    memory.put(KEY, [next, ...list]);
    emit();
    return next;
  }, []);

  return { items, add };
}
