FROM node:24.11.1-bookworm-slim AS build
WORKDIR /app
ENV PNPM_HOME=/pnpm \
    PATH=/pnpm:$PATH \
    NEXT_TELEMETRY_DISABLED=1 \
    API_ORIGIN=http://api:8080 \
    API_BASE_URL=http://api:8080
RUN corepack enable && corepack prepare pnpm@10.15.0 --activate
COPY apps/web/package.json apps/web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY apps/web/ ./
RUN pnpm build

FROM node:24.11.1-bookworm-slim AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    HOSTNAME=0.0.0.0 \
    PORT=8080
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
USER node
EXPOSE 8080
CMD ["node", "server.js"]
