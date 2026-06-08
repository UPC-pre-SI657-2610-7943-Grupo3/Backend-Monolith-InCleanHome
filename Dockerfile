FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["InCleanHome.API/InCleanHome.API.csproj", "InCleanHome.API/"]
RUN dotnet restore "InCleanHome.API/InCleanHome.API.csproj"
COPY . .
WORKDIR "/src/InCleanHome.API"
RUN dotnet build "InCleanHome.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "InCleanHome.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InCleanHome.API.dll"]