SERVICES := identity content quiz progress notification
CLUSTER_NAME := wordbuddy
SERVICE ?=

# Database provider (ADR 0009): sqlserver (default) or postgres. Picks the EF context for migrate / k8s-migrate
# and the manifests for k8s-apply.
DB ?= sqlserver
ifeq ($(DB),sqlserver)
DB_CONTEXT_PREFIX := SqlServer
else ifeq ($(DB),postgres)
DB_CONTEXT_PREFIX := Postgres
else
$(error DB must be sqlserver or postgres, got '$(DB)')
endif
# identity -> Identity (GNU sed, same trick as k8s-build-load)
SERVICE_PASCAL = $(shell echo $(SERVICE) | sed 's/./\U&/')
EF_CONTEXT = $(DB_CONTEXT_PREFIX)$(SERVICE_PASCAL)DbContext

COMPOSE_POSTGRES := docker compose -f docker-compose.yml -f docker-compose.postgres.yml

.PHONY: up up-postgres down down-postgres logs migrate clean pack-shared publish-shared \
        cluster-up cluster-down k8s-build-load k8s-apply k8s-logs k8s-migrate k8s-rabbitmq-ui \
        k8s-prod-render k8s-prod-apply k8s-prod-health

## --- docker-compose (local dev inner loop) ---

up:
	docker compose up --build

# PostgreSQL instead of SQL Server (docker-compose.postgres.yml; needs the *_PG_CONNECTION_STRING values from .env.example).
up-postgres:
	$(COMPOSE_POSTGRES) up --build

down:
	docker compose down

down-postgres:
	$(COMPOSE_POSTGRES) down

logs:
	docker compose logs -f $(SERVICE)

migrate:
	docker compose exec $(SERVICE)-api dotnet ef database update --context $(EF_CONTEXT)

clean:
	docker compose down -v

## --- shared library packages ---

pack-shared:
	dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed
	dotnet pack src/Shared/WordBuddy.Shared.Application -o local-nuget-feed
	dotnet pack src/Shared/WordBuddy.Shared.Infrastructure -o local-nuget-feed
	dotnet pack src/Shared/WordBuddy.Shared.Contracts -o local-nuget-feed
	dotnet pack src/Shared/WordBuddy.Shared.Protos -o local-nuget-feed

publish-shared:
	for f in local-nuget-feed/*.nupkg; do \
		dotnet nuget push "$$f" \
			--source github-wordbuddy \
			--api-key $(GITHUB_TOKEN) \
			--skip-duplicate || exit 1; \
	done

## --- local Kubernetes (kind) ---

cluster-up:
	kind create cluster --name $(CLUSTER_NAME) --config kind-config.yaml
	kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
	kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=120s

cluster-down:
	kind delete cluster --name $(CLUSTER_NAME)

k8s-build-load:
ifeq ($(SERVICE),)
	@for s in $(SERVICES); do \
		svc=$$(echo $$s | sed 's/./\U&/'); \
		docker build --secret id=github_token,env=GITHUB_TOKEN -t wordbuddy-$$s:dev src/Services/$$svc; \
		kind load docker-image wordbuddy-$$s:dev --name $(CLUSTER_NAME); \
	done
	docker build -t wordbuddy-ui:dev -f ../WordBuddy.UI/Dockerfile ../WordBuddy.UI
	kind load docker-image wordbuddy-ui:dev --name $(CLUSTER_NAME)
else
	svc=$$(echo $(SERVICE) | sed 's/./\U&/'); \
	docker build --secret id=github_token,env=GITHUB_TOKEN -t wordbuddy-$(SERVICE):dev src/Services/$$svc; \
	kind load docker-image wordbuddy-$(SERVICE):dev --name $(CLUSTER_NAME)
endif

# DB=postgres swaps the SQL Server files (configmap, secret, database StatefulSet, Service, init Job) for k8s/postgres/.
k8s-apply:
	kubectl apply -f k8s/namespace.yaml
ifeq ($(DB),postgres)
	kubectl apply -f k8s/postgres/configmap.yaml -f k8s/postgres/secret.yaml
	kubectl apply -f k8s/postgres/postgres-statefulset.yaml -f k8s/postgres/postgres-service.yaml
	kubectl apply -f k8s/postgres/postgres-init-job.yaml
else
	kubectl apply -f k8s/configmap.yaml -f k8s/secret.yaml
	kubectl apply -f k8s/sqlserver-statefulset.yaml -f k8s/sqlserver-service.yaml
	kubectl apply -f k8s/sqlserver-init-job.yaml
endif
	kubectl apply -f k8s/rabbitmq-deployment.yaml -f k8s/rabbitmq-service.yaml
	kubectl apply -f k8s/identity-deployment.yaml -f k8s/identity-service.yaml
	kubectl apply -f k8s/content-deployment.yaml -f k8s/content-service.yaml
	kubectl apply -f k8s/quiz-deployment.yaml -f k8s/quiz-service.yaml
	kubectl apply -f k8s/progress-deployment.yaml -f k8s/progress-service.yaml
	kubectl apply -f k8s/notification-deployment.yaml -f k8s/notification-service.yaml
	kubectl apply -f k8s/ui-deployment.yaml -f k8s/ui-service.yaml
	kubectl apply -f k8s/ingress.yaml

k8s-rabbitmq-ui:
	kubectl -n wordbuddy port-forward svc/rabbitmq 15672:15672

k8s-logs:
	kubectl -n wordbuddy logs -f deploy/$(SERVICE)-api

k8s-migrate:
	kubectl -n wordbuddy exec deploy/$(SERVICE)-api -- dotnet ef database update --context $(EF_CONTEXT)

## --- Kubernetes production overlay (ADR 0010, WB-75) ---
# Needs k8s/production/secret.env (copy secret.env.example). Uses the ACTIVE kube context.
# The overlay reads manifests one level up, so kustomize runs with --load-restrictor=LoadRestrictionsNone.
KUSTOMIZE_PROD := kubectl kustomize --load-restrictor=LoadRestrictionsNone k8s/production
OWNER ?=
VERSION ?=

# Renders the overlay (no cluster access). Also checks that both copies of the Postgres init script are identical.
k8s-prod-render:
	@diff -q deploy/single-host/postgres/init.sh k8s/production/postgres-init.sh
	$(KUSTOMIZE_PROD)

# make k8s-prod-apply OWNER=<github-owner> VERSION=1.0.0 -- shows the kube context and asks first.
k8s-prod-apply:
	@test -n "$(VERSION)" || { echo "VERSION is required, e.g. make k8s-prod-apply OWNER=me VERSION=1.0.0"; exit 1; }
	@test -n "$(OWNER)" || { echo "OWNER (GitHub owner, lower case) is required"; exit 1; }
	@diff -q deploy/single-host/postgres/init.sh k8s/production/postgres-init.sh
	@echo "kube context: $$(kubectl config current-context)"
	@echo "Deploying WordBuddy $(VERSION) from ghcr.io/$(OWNER) to this cluster."
	@read -p "Continue? [y/N] " answer; [ "$$answer" = "y" ] || { echo "cancelled"; exit 1; }
	$(KUSTOMIZE_PROD) | sed -e 's#ghcr.io/OWNER/#ghcr.io/$(OWNER)/#g' -e 's#\(ghcr.io/[^ ]*wordbuddy-[a-z]*\):0.0.0#\1:$(VERSION)#g' > k8s/production/.rendered.yaml
	kubectl -n wordbuddy delete job postgres-init --ignore-not-found
	kubectl apply -f k8s/production/.rendered.yaml

# Waits for every Deployment (readiness = /health/ready) and lists the pods.
k8s-prod-health:
	@echo "kube context: $$(kubectl config current-context)"
	@for d in rabbitmq identity-api content-api quiz-api progress-api notification-api wordbuddy-ui; do \
		kubectl -n wordbuddy rollout status deploy/$$d --timeout=180s || exit 1; \
	done
	kubectl -n wordbuddy get pods
