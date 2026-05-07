FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/Giwu.Api/Giwu.Api.csproj", "src/Giwu.Api/"]
COPY ["src/Giwu.Application/Giwu.Application.csproj", "src/Giwu.Application/"]
COPY ["src/Giwu.Infrastructure/Giwu.Infrastructure.csproj", "src/Giwu.Infrastructure/"]
COPY ["src/Giwu.Contracts/Giwu.Contracts.csproj", "src/Giwu.Contracts/"]
COPY ["src/Giwu.Domain/Giwu.Domain.csproj", "src/Giwu.Domain/"]
RUN dotnet restore "src/Giwu.Api/Giwu.Api.csproj"
COPY . .
WORKDIR "/src/src/Giwu.Api"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Giwu.Api.dll"]