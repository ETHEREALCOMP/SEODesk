FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

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

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SEODesk.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN apt-get update && \
    apt-get install -y libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*

USER $APP_UID
ENTRYPOINT ["dotnet", "SEODesk.API.dll"]