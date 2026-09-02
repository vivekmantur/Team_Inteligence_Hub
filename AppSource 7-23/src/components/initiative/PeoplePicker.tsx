import { useMemo, useState } from "react";
import { teamMembers } from "@/data/mock";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface Props {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  className?: string;
}

export function PeoplePickerInput({ value, onChange, placeholder, className }: Props) {
  const [q, setQ] = useState("");
  const [open, setOpen] = useState(false);

  const suggestions = useMemo(() => {
    const term = (q || value).toLowerCase();
    return teamMembers.filter((m) => m.name.toLowerCase().includes(term)).slice(0, 6);
  }, [q, value]);

  return (
    <div className={cn("relative", className)}>
      <Input
        value={q || value}
        onChange={(e) => {
          setQ(e.target.value);
          onChange(e.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 120)}
        placeholder={placeholder}
        className="h-10 rounded-xl bg-white/80 border-white/70"
      />
      {open && suggestions.length > 0 && (
        <div className="absolute z-20 mt-1 w-full rounded-xl bg-white border border-black/5 shadow-xl overflow-hidden">
          {suggestions.map((m) => (
            <button
              key={m.id}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => {
                onChange(m.name);
                setQ("");
                setOpen(false);
              }}
              className="w-full text-left px-3 py-2 hover:bg-muted flex items-center gap-2"
            >
              <div
                className={cn(
                  "size-7 rounded-full text-white text-[11px] font-semibold grid place-items-center bg-gradient-to-br",
                  m.avatarColor
                )}
              >
                {initials(m.name)}
              </div>
              <div className="min-w-0">
                <div className="text-[13px] font-medium truncate">{m.name}</div>
                <div className="text-[11px] text-muted-foreground truncate">{m.role}</div>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export function initials(name: string) {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

export function avatarColorFor(name: string) {
  const known = teamMembers.find((m) => m.name.toLowerCase() === name.toLowerCase());
  if (known) return known.avatarColor;
  const palette = [
    "from-indigo-500 to-fuchsia-500",
    "from-sky-500 to-indigo-500",
    "from-rose-500 to-orange-500",
    "from-emerald-500 to-teal-500",
    "from-fuchsia-500 to-purple-500",
    "from-amber-500 to-rose-500",
  ];
  let sum = 0;
  for (let i = 0; i < name.length; i++) sum += name.charCodeAt(i);
  return palette[sum % palette.length];
}
