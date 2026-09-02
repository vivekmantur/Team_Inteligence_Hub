import { useMemo, useState } from "react";
import { X } from "lucide-react";
import { Input } from "@/components/ui/input";
import { useUsers } from "@/hooks/use-users";
import { cn } from "@/lib/utils";

type Props = {
  value: number | null;
  onChange: (value: number | null) => void;
  placeholder?: string;
  /** Hide someone already chosen elsewhere, e.g. the Owner when picking a Sponsor. */
  excludeUserId?: number | null;
  /** Ids already on the team, hidden so the same person cannot be added twice. */
  excludeUserIds?: number[];
  className?: string;
};

/**
 * Selects a real user and reports their id.
 *
 * Only people who have signed in at least once appear, because every place this feeds
 * is a foreign key onto the Users table — a typed name cannot satisfy one.
 */
export function UserPicker({
  value,
  onChange,
  placeholder,
  excludeUserId,
  excludeUserIds,
  className,
}: Props) {
  const { data: users = [], isLoading, error } = useUsers();
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);

  const selected = users.find((u) => u.id === value) ?? null;

  const suggestions = useMemo(() => {
    const term = query.trim().toLowerCase();
    const hidden = new Set(excludeUserIds ?? []);
    if (excludeUserId != null) hidden.add(excludeUserId);

    return users
      .filter((u) => !hidden.has(u.id))
      .filter(
        (u) =>
          !term ||
          u.displayName.toLowerCase().includes(term) ||
          u.email.toLowerCase().includes(term)
      )
      .slice(0, 6);
  }, [users, query, excludeUserId, excludeUserIds]);

  return (
    <div className={cn("relative", className)}>
      <div className="relative">
        <Input
          value={open ? query : selected?.displayName ?? ""}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onFocus={() => {
            setQuery("");
            setOpen(true);
          }}
          onBlur={() => setTimeout(() => setOpen(false), 120)}
          placeholder={isLoading ? "Loading people…" : placeholder}
          className="h-10 rounded-xl bg-white/80 border-white/70 pr-9"
        />
        {selected && !open && (
          <button
            type="button"
            aria-label="Clear selection"
            onClick={() => onChange(null)}
            className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
          >
            <X className="size-4" />
          </button>
        )}
      </div>

      {error && (
        <p className="mt-1 text-[11px] text-amber-700">
          Could not load people. Only those who have signed in can be selected.
        </p>
      )}

      {open && (
        <div className="absolute z-30 mt-1 w-full rounded-xl bg-white border border-black/5 shadow-xl overflow-hidden">
          {suggestions.length === 0 && (
            <p className="px-3 py-2 text-[12px] text-muted-foreground">
              {isLoading
                ? "Loading…"
                : "No match. People appear here after they sign in once."}
            </p>
          )}
          {suggestions.map((u) => (
            <button
              key={u.id}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => {
                onChange(u.id);
                setQuery("");
                setOpen(false);
              }}
              className="w-full text-left px-3 py-2 hover:bg-muted flex items-center gap-2"
            >
              <div className="size-7 rounded-full text-white text-[11px] font-semibold grid place-items-center bg-gradient-to-br from-indigo-500 to-fuchsia-500">
                {u.displayName
                  .split(" ")
                  .map((n) => n[0])
                  .join("")
                  .slice(0, 2)
                  .toUpperCase()}
              </div>
              <div className="min-w-0">
                <div className="text-[13px] font-medium truncate">{u.displayName}</div>
                <div className="text-[11px] text-muted-foreground truncate">{u.email}</div>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export default UserPicker;
