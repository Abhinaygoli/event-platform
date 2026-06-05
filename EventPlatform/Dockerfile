# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files first (for layer caching)
COPY EventPlatform.slnx ./
COPY src/EventPlatform.API/*.csproj ./src/EventPlatform.API/
COPY src/EventPlatform.Application/*.csproj ./src/EventPlatform.Application/
COPY src/EventPlatform.Domain/*.csproj ./src/EventPlatform.Domain/
COPY src/EventPlatform.Infrastructure/*.csproj ./src/EventPlatform.Infrastructure/
RUN dotnet restore EventPlatform.slnx

# Copy all source code and publish
COPY src/ ./src/
RUN dotnet publish src/EventPlatform.API/EventPlatform.API.csproj \
    -c Release -o /app/out

# Stage 2: Runtime image (much smaller than SDK image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Render free tier uses port 10000
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "EventPlatform.API.dll"]
