.PHONY: up down logs migrate clean help

COMPOSE  := docker compose
# Docker Compose project name is set to "wordbuddy" in docker-compose.yml,
# so the internal network is always "wordbuddy_backend" regardless of the
# directory this Makefile lives in.
NETWORK  := wordbuddy_backend
SDK_IMG  := mcr.microsoft.com/dotnet/sdk:8.0

# Read CONNECTION_STRING from .env so migrate can pass it into the container.
-include .env
export

## up: Build images and start all services in the background
up:
	$(COMPOSE) up --build -d

## down: Stop and remove containers (volumes are preserved)
down:
	$(COMPOSE) down

## logs: Stream logs from all services  (Ctrl-C to stop)
logs:
	$(COMPOSE) logs -f

## migrate: Run EF Core migrations against the running SQL Server container
##          Spins up a temporary .NET SDK container on the same network.
##          Requires sqlserver to be healthy (run 'make up' first).
migrate:
	docker run --rm \
		--network $(NETWORK) \
		-v "$(CURDIR)/WordBuddy:/src" \
		-e "ConnectionStrings__Default=$(CONNECTION_STRING)" \
		$(SDK_IMG) \
		sh -c '\
			cd /src && \
			dotnet tool install --global dotnet-ef --version "8.*" 2>/dev/null; \
			export PATH="$$PATH:/root/.dotnet/tools"; \
			dotnet ef database update \
				--project src/WordBuddy.Infrastructure \
				--startup-project src/WordBuddy.API \
		'

## clean: Stop containers AND delete all volumes (data will be lost!)
clean:
	$(COMPOSE) down -v

## help: List available targets
help:
	@grep -E '^## ' Makefile | sed 's/## //'
