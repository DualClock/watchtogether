FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/WatchTogether.Api/WatchTogether.Api/WatchTogether.Api.csproj", "src/WatchTogether.Api/WatchTogether.Api/"]
COPY ["src/WatchTogether.Application/WatchTogether.Application/WatchTogether.Application.csproj", "src/WatchTogether.Application/WatchTogether.Application/"]
COPY ["src/WatchTogether.Core/WatchTogether.Core/WatchTogether.Core.csproj", "src/WatchTogether.Core/WatchTogether.Core/"]
COPY ["src/WatchTogether.Infrastructure/WatchTogether.Infrastructure/WatchTogether.Infrastructure.csproj", "src/WatchTogether.Infrastructure/WatchTogether.Infrastructure/"]
RUN dotnet restore "src/WatchTogether.Api/WatchTogether.Api/WatchTogether.Api.csproj"
COPY . .
WORKDIR "/src/src/WatchTogether.Api/WatchTogether.Api"
RUN dotnet build "WatchTogether.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WatchTogether.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WatchTogether.Api.dll"]
