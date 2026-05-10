# Backend build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src

# Copy all source
COPY backend/oftic/ ./

# Restore and publish API project directly to avoid solution path issues
RUN dotnet restore oftic/Api.csproj
RUN dotnet publish oftic/Api.csproj -c Release -o /app/publish --no-restore

# Backend runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api-runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create uploads directory and set permissions
RUN mkdir -p /app/uploads/noticias && \
    mkdir -p /app/uploads/sliders && \
    mkdir -p /app/uploads/login && \
    mkdir -p /app/uploads/videos

COPY --from=api-build /app/publish .

EXPOSE 8088

ENV ASPNETCORE_URLS=http://+:8088
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8088/api/auth-test/cache-status || exit 1

ENTRYPOINT ["dotnet", "Api.dll"]

# Frontend build stage
FROM node:20-alpine AS frontend-build
WORKDIR /web

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

# Frontend runtime stage
FROM nginx:alpine AS frontend-runtime
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=frontend-build /web/dist/policiadev-app /usr/share/nginx/html
EXPOSE 80
