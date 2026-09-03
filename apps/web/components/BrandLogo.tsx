type BrandLogoProps = {
  compact?: boolean;
  className?: string;
};

export function BrandLogo({ compact = false, className = "" }: BrandLogoProps) {
  const classes = ["brand-logo", compact ? "brand-logo-compact" : "", className]
    .filter(Boolean)
    .join(" ");

  return (
    <span className={classes}>
      <img
        className="brand-logo-wordmark"
        src="/brand/deal-north-logo.png"
        width="1448"
        height="1086"
        alt="Deal North"
      />
      <img className="brand-logo-mark" src="/icon.svg" width="64" height="64" alt="" aria-hidden="true" />
      <span className="brand-logo-mobile-name" aria-hidden="true">Deal North</span>
    </span>
  );
}
