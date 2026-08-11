type StateBadgeProps = { label: string; tone?: "good" | "neutral" | "warning" | "muted" };

export function StateBadge({ label, tone = "neutral" }: StateBadgeProps) {
  return <span className={`state-badge state-${tone}`}>{label}</span>;
}

export function freshnessTone(state: string) {
  if (state === "RECENT") return "good" as const;
  if (state === "STALE") return "warning" as const;
  if (state === "UNKNOWN") return "muted" as const;
  return "neutral" as const;
}
