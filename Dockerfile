# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["FinTrack.csproj", "./"]
RUN dotnet restore "./FinTrack.csproj"

# Copy the rest of the application files and publish
COPY . .
RUN dotnet publish "./FinTrack.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Configure default port for container runtime (Render detects port 8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinTrack.dll"]
