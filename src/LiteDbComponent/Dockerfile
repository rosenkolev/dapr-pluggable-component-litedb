FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
##RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
##USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LiteDbComponent.csproj", "app/"]
RUN dotnet restore "app/LiteDbComponent.csproj"
COPY . app/
WORKDIR "/src/app"
RUN dotnet build "LiteDbComponent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LiteDbComponent.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiteDbComponent.dll"]
