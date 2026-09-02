import { ReactNode, useEffect, useState } from "react";
import { TrendingUp, TrendingDown } from "lucide-react";
import { cn } from "@/lib/utils";

interface KpiCardProps {
  label: string;
  value: number;
  suffix?: string;
  trend?: number;
  icon: ReactNode;
  accent: string; // gradient tailwind classes
  format?: "number" | "compact";
}

function useCountUp(target: number, duration = 900) {
  const [v, setV] = useState(0);
  useEffect(() => {
    let raf = 0;
    const start = performance.now();
    const tick = (t: number) => {
      const p = Math.min(1, (t - start) / duration);
      const eased = 1 - Math.pow(1 - p, 3);
      setV(Math.round(target * eased));
      if (p < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [target, duration]);
  return v;
}

function formatNum(n: number, mode: "number" | "compact") {
  if (mode === "compact") {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + "M";
    if (n >= 1_000) return (n / 1_000).toFixed(1) + "K";
  }
  return n.toLocaleString();
}

export function KpiCard({ label, value, suffix, trend, icon, accent, format = "number" }: KpiCardProps) {
  const v = useCountUp(value);
  const up = (trend ?? 0) >= 0;
  return (
    <div className="glass rounded-2xl p-4 relative overflow-hidden group hover:shadow-xl transition-shadow">
      <div className={cn("absolute -top-10 -right-10 size-32 rounded-full opacity-40 blur-2xl", accent)} />
      <div className="flex items-start justify-between">
        <div className={cn("size-10 rounded-xl grid place-items-center text-white shadow-md", accent)}>
          {icon}
        </div>
        {trend !== undefined && (
          <div
            className={cn(
              "text-[11px] font-semibold px-2 py-0.5 rounded-full inline-flex items-center gap-1",
              up ? "bg-emerald-500/10 text-emerald-700" : "bg-rose-500/10 text-rose-700"
            )}
          >
            {up ? <TrendingUp className="size-3" /> : <TrendingDown className="size-3" />}
            {up ? "+" : ""}
            {trend}%
          </div>
        )}
      </div>
      <div className="mt-4">
        <div className="text-2xl md:text-3xl font-semibold tracking-tight">
          {formatNum(v, format)}
          {suffix && <span className="text-base text-muted-foreground font-medium ml-1">{suffix}</span>}
        </div>
        <div className="text-[13px] text-muted-foreground mt-1">{label}</div>
      </div>
    </div>
  );
}
