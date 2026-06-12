FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY src/CommissionService/CommissionService.csproj CommissionService/
COPY src/Shared/PartnerSystem.Contracts/PartnerSystem.Contracts.csproj Shared/PartnerSystem.Contracts/
COPY src/Shared/PartnerSystem.Contracts/PartnerService.proto Shared/PartnerSystem.Contracts/

WORKDIR /src/CommissionService
RUN dotnet restore

COPY src/CommissionService/ ./
COPY src/Shared/PartnerSystem.Contracts/ ../Shared/PartnerSystem.Contracts/

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CommissionService.dll"]