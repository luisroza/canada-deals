"use client";

import Link from "next/link";
import { useState } from "react";
import { useWishlist } from "./WishlistContext";

export function WishlistCardButton({ productId, productTitle, returnTo }: { productId: string; productTitle: string; returnTo: string }) {
  const wishlist = useWishlist();
  const [message, setMessage] = useState("");
  const saved = wishlist.isSaved(productId);
  const pending = wishlist.isPending(productId);

  if (wishlist.loading) {
    return <button className="card-wishlist-button" type="button" disabled aria-label={`Save — loading Wishlist state for ${productTitle}`}><span className="card-wishlist-icon" aria-hidden="true">♡</span> <span className="card-wishlist-label">Save</span></button>;
  }

  if (wishlist.authenticated === false) {
    return <Link className="card-wishlist-button" href={`/account/sign-in?returnTo=${encodeURIComponent(returnTo)}`} aria-label={`Save ${productTitle} to your Wishlist — sign in required`}><span className="card-wishlist-icon" aria-hidden="true">♡</span> <span className="card-wishlist-label">Save</span></Link>;
  }

  if (wishlist.authenticated !== true) {
    return <button className="card-wishlist-button" type="button" disabled aria-label={`Save — Wishlist unavailable for ${productTitle}`}><span className="card-wishlist-icon" aria-hidden="true">♡</span> <span className="card-wishlist-label">Save</span></button>;
  }

  async function toggle() {
    setMessage("");
    try {
      const nowSaved = await wishlist.toggle(productId);
      setMessage(nowSaved ? "Saved to your Wishlist." : "Removed from your Wishlist.");
    } catch {
      setMessage("Wishlist could not be updated. Try again.");
    }
  }

  return <>
    <button className="card-wishlist-button" type="button" disabled={pending} aria-pressed={saved} aria-label={pending ? `Wait — ${saved ? "removing" : "saving"} ${productTitle} ${saved ? "from" : "to"} your Wishlist` : saved ? `Saved — remove ${productTitle} from your Wishlist` : `Save ${productTitle} to your Wishlist`} onClick={toggle}><span className="card-wishlist-icon" aria-hidden="true">{saved ? "♥" : "♡"}</span> <span className="card-wishlist-label">{pending ? "Wait" : saved ? "Saved" : "Save"}</span></button>
    <span className="sr-only" role="status" aria-live="polite">{message}</span>
  </>;
}
