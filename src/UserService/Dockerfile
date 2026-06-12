FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY src/UserService/UserService.csproj UserService/
COPY src/Shared/PartnerSystem.Contracts/PartnerSystem.Contracts.csproj Shared/PartnerSystem.Contracts/
COPY src/Shared/PartnerSystem.Contracts/PartnerService.proto Shared/PartnerSystem.Contracts/

WORKDIR /src/UserService
RUN dotnet restore

COPY src/UserService/. ./
COPY src/Shared/PartnerSystem.Contracts/. ../Shared/PartnerSystem.Contracts/

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080 8081

# ❌ УБРАЛИ: ENV ASPNETCORE_URLS=http://+:8080
# Используем ConfigureKestrel из Program.cs

ENTRYPOINT ["dotnet", "UserService.dll"]