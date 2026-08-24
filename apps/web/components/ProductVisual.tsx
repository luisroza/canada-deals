"use client";

import { useState } from "react";
import type { ProductImageData } from "../lib/api";

export function ProductVisual({ image, title, category, className = "product-image" }: { image: ProductImageData | null; title: string; category: string; className?: string }) {
  const [failed, setFailed] = useState(false);
  if (!image || failed) {
    return <span className="product-image-fallback" role="img" aria-label={`No image available for ${title}`}><strong aria-hidden="true">{category.slice(0, 2).toUpperCase()}</strong><small>{category}</small></span>;
  }
  return <img className={className} src={image.url} width={image.width} height={image.height} alt={title} loading="lazy" decoding="async" onError={() => setFailed(true)} />;
}
