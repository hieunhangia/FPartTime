# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Repository/Repository.csproj", "Repository/"]
COPY ["DbMigrator/DbMigrator.csproj", "DbMigrator/"]
RUN dotnet restore "DbMigrator/DbMigrator.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish release output
WORKDIR "/src/DbMigrator"
RUN dotnet publish "DbMigrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DbMigrator.dll"]
