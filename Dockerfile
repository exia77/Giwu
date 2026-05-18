# syntax=docker/dockerfile:1.6
# ─────────────────────────────────────────────────────────────────────
# Repo-root Dockerfile for the Giwu HRMS API.
#
# Lives at the repo root so platforms that look for a Dockerfile in the
# default location (Render, Fly.io, Cloud Run, etc.) find it without
# extra path configuration. Build context is the repo root, so all COPY
# instructions are relative to that.
#
# Mirrors Giwu.Api/deploy/Dockerfile (which is kept for builds run from
# the Giwu.Api/ directory locally).
# ─────────────────────────────────────────────────────────────────────
ARG DOTNET_SDK_TAG=10.0-bookworm-slim
ARG DOTNET_RUNTIME_TAG=10.0-bookworm-slim

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_TAG} AS build
WORKDIR /src

# Copy csproj files first for cached restore.
COPY Giwu.Api/Directory.Build.props                       Giwu.Api/
COPY Giwu.Api/src/Giwu.Domain/*.csproj                    Giwu.Api/src/Giwu.Domain/
COPY Giwu.Api/src/Giwu.Contracts/*.csproj                 Giwu.Api/src/Giwu.Contracts/
COPY Giwu.Api/src/Giwu.Application/*.csproj               Giwu.Api/src/Giwu.Application/
COPY Giwu.Api/src/Giwu.Infrastructure/*.csproj            Giwu.Api/src/Giwu.Infrastructure/
COPY Giwu.Api/src/Giwu.Api/*.csproj                       Giwu.Api/src/Giwu.Api/
RUN dotnet restore Giwu.Api/src/Giwu.Api/Giwu.Api.csproj

# Now copy the rest of the API source. We deliberately don't copy
# Giwu.HRMS.Hybrid/ — the MAUI client isn't part of the API image and
# pulling it in would (a) bloat the layer and (b) fail because the SDK
# image doesn't have the MAUI workload installed.
COPY Giwu.Api/ Giwu.Api/

RUN dotnet publish Giwu.Api/src/Giwu.Api/Giwu.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_TAG} AS runtime
WORKDIR /app

# Bind to whatever PORT the platform injects (Render injects PORT=10000
# by default). EXPOSE is metadata only — the actual listening port comes
# from $PORT at start time. The fallback to 8080 keeps `docker run`
# without env vars working locally.
ENV PORT=8080
EXPOSE 8080

# Run as non-root.
RUN groupadd --system app && useradd --system --gid app --uid 1001 app
COPY --from=build --chown=app:app /app/publish .
USER app

# Shell form so $PORT expands at container start, not at image-build time.
ENTRYPOINT ["sh","-c","ASPNETCORE_URLS=http://+:${PORT} dotnet Giwu.Api.dll"]
