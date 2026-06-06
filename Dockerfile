# Stage 1 — build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so the restore layer is only invalidated when
# a .csproj file changes, not on every source code change.
COPY ["src/WordBuddy.Domain/WordBuddy.Domain.csproj",           "src/WordBuddy.Domain/"]
COPY ["src/WordBuddy.Application/WordBuddy.Application.csproj", "src/WordBuddy.Application/"]
COPY ["src/WordBuddy.Infrastructure/WordBuddy.Infrastructure.csproj", "src/WordBuddy.Infrastructure/"]
COPY ["src/WordBuddy.API/WordBuddy.API.csproj",                 "src/WordBuddy.API/"]

RUN dotnet restore "src/WordBuddy.API/WordBuddy.API.csproj"

# Copy remaining source and publish
COPY . .
RUN dotnet publish "src/WordBuddy.API/WordBuddy.API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Stage 2 — runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && \
    useradd --system --no-create-home --gid appgroup appuser

COPY --from=build --chown=appuser:appgroup /app/publish ./

USER appuser

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "WordBuddy.API.dll"]
