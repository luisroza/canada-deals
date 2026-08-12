export const dynamic = "force-dynamic";

export function GET() {
  return Response.json(
    { status: "healthy", component: "web" },
    { headers: { "Cache-Control": "no-store" } },
  );
}
