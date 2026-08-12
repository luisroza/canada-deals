FROM mcr.microsoft.com/dotnet/sdk:10.0.300 AS build
WORKDIR /src
COPY CanadaDeals.slnx ./
COPY src/backend/CanadaDeals.Domain/CanadaDeals.Domain.csproj src/backend/CanadaDeals.Domain/
COPY src/backend/CanadaDeals.Infrastructure/CanadaDeals.Infrastructure.csproj src/backend/CanadaDeals.Infrastructure/
COPY src/backend/CanadaDeals.Api/CanadaDeals.Api.csproj src/backend/CanadaDeals.Api/
COPY src/backend/CanadaDeals.Worker/CanadaDeals.Worker.csproj src/backend/CanadaDeals.Worker/
COPY tests/CanadaDeals.Domain.Tests/CanadaDeals.Domain.Tests.csproj tests/CanadaDeals.Domain.Tests/
COPY tests/CanadaDeals.Api.IntegrationTests/CanadaDeals.Api.IntegrationTests.csproj tests/CanadaDeals.Api.IntegrationTests/
RUN dotnet restore CanadaDeals.slnx
COPY src/backend src/backend
RUN dotnet publish src/backend/CanadaDeals.Worker/CanadaDeals.Worker.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.8 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CanadaDeals.Worker.dll"]
