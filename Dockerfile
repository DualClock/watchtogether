FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WatchTogether.Backend/src/WatchTogether.Api/WatchTogether.Api/WatchTogether.Api.csproj", "WatchTogether.Backend/src/WatchTogether.Api/WatchTogether.Api/"]
COPY ["WatchTogether.Backend/src/WatchTogether.Application/WatchTogether.Application/WatchTogether.Application.csproj", "WatchTogether.Backend/src/WatchTogether.Application/WatchTogether.Application/"]
COPY ["WatchTogether.Backend/src/WatchTogether.Core/WatchTogether.Core/WatchTogether.Core.csproj", "WatchTogether.Backend/src/WatchTogether.Core/WatchTogether.Core/"]
COPY ["WatchTogether.Backend/src/WatchTogether.Infrastructure/WatchTogether.Infrastructure/WatchTogether.Infrastructure.csproj", "WatchTogether.Backend/src/WatchTogether.Infrastructure/WatchTogether.Infrastructure/"]
RUN dotnet restore "WatchTogether.Backend/src/WatchTogether.Api/WatchTogether.Api/WatchTogether.Api.csproj"
COPY . .
WORKDIR "/src/WatchTogether.Backend/src/WatchTogether.Api/WatchTogether.Api"
RUN dotnet build "WatchTogether.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WatchTogether.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WatchTogether.Api.dll"]
