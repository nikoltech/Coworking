FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Coworking/src/Coworking.API/Coworking.API.csproj", "Coworking/src/Coworking.API/"]
COPY ["Coworking/src/Coworking.Application/Coworking.Application.csproj", "Coworking/src/Coworking.Application/"]
COPY ["Coworking/src/Coworking.Domain/Coworking.Domain.csproj", "Coworking/src/Coworking.Domain/"]
COPY ["Coworking/src/Coworking.External/Squidex/Coworking.External.Squidex.Abstractions/Coworking.External.Squidex.Abstractions.csproj", "Coworking/src/Coworking.External/Squidex/Coworking.External.Squidex.Abstractions/"]
COPY ["Coworking/src/Coworking.Infrastructure.Persistence/Coworking.Infrastructure.Persistence.csproj", "Coworking/src/Coworking.Infrastructure.Persistence/"]
COPY ["Coworking/src/Coworking.Infrastructure/Coworking.Infrastructure.csproj", "Coworking/src/Coworking.Infrastructure/"]
COPY ["Coworking/src/Coworking.External/Squidex/Coworking.External.Squidex/Coworking.External.Squidex.csproj", "Coworking/src/Coworking.External/Squidex/Coworking.External.Squidex/"]

RUN dotnet restore "./Coworking/src/Coworking.API/Coworking.API.csproj"
COPY . .
WORKDIR "/src/Coworking/src/Coworking.API"
RUN dotnet build "./Coworking.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Coworking.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Coworking.API.dll"]