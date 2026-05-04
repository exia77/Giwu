# syntax=docker/dockerfile:1.6
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy csproj files first for cached restore
COPY Directory.Build.props ./
COPY src/Giwu.Domain/*.csproj         src/Giwu.Domain/
COPY src/Giwu.Contracts/*.csproj      src/Giwu.Contracts/
COPY src/Giwu.Application/*.csproj    src/Giwu.Application/
COPY src/Giwu.Infrastructure/*.csproj src/Giwu.Infrastructure/
COPY src/Giwu.Api/*.csproj            src/Giwu.Api/
RUN dotnet restore src/Giwu.Api/Giwu.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/Giwu.Api/Giwu.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as non-root
RUN groupadd --system app && useradd --system --gid app --uid 1001 app
COPY --from=build --chown=app:app /app/publish .
USER app

ENTRYPOINT ["dotnet","Giwu.Api.dll"]
