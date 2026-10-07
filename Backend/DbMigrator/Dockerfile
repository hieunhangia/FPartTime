# Stage 1: Build & Publish (chạy native trên host runner x86 để đạt tốc độ tối đa)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy csproj files and restore dependencies cho target architecture
COPY ["Repository/Repository.csproj", "Repository/"]
COPY ["DbMigrator/DbMigrator.csproj", "DbMigrator/"]
RUN dotnet restore "DbMigrator/DbMigrator.csproj" -a $TARGETARCH

# Copy the rest of the source code
COPY . .

# Build and publish release output trực tiếp cho target architecture (không cần QEMU)
WORKDIR "/src/DbMigrator"
RUN dotnet publish "DbMigrator.csproj" -c Release -a $TARGETARCH -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime environment (tự động khớp với TARGETARCH)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

USER app

ENTRYPOINT ["dotnet", "DbMigrator.dll"]
