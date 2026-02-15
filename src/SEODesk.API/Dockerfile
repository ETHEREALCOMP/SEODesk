# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/SEODesk.API/SEODesk.API.csproj", "src/SEODesk.API/"]
COPY ["src/SEODesk.Application/SEODesk.Application.csproj", "src/SEODesk.Application/"]
COPY ["src/SEODesk.Domain/SEODesk.Domain.csproj", "src/SEODesk.Domain/"]
COPY ["src/SEODesk.Infrastructure/SEODesk.Infrastructure.csproj", "src/SEODesk.Infrastructure/"]
RUN dotnet restore "./src/SEODesk.API/SEODesk.API.csproj"
COPY . .
WORKDIR "/src/src/SEODesk.API"
RUN dotnet build "./SEODesk.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

RUN mkdir -p /persistent-keys && chown -R $APP_UID:$APP_UID /persistent-keys

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SEODesk.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SEODesk.API.dll"]