# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["BackendApiClient/BackendApiClient.csproj", "BackendApiClient/"]
COPY ["Frontend/Frontend.csproj", "Frontend/"]
RUN dotnet restore "Frontend/Frontend.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish release output
WORKDIR "/src/Frontend"
RUN dotnet publish "Frontend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Frontend.dll"]
