SERVICES := identity content quiz progress notification
CLUSTER_NAME := wordbuddy
SERVICE ?=

.PHONY: up down logs migrate clean pack-shared publish-shared \
        cluster-up cluster-down k8s-build-load k8s-apply k8s-logs k8s-migrate

## --- docker-compose (local dev inner loop) ---

up:
	docker compose up --build

down:
	docker compose down

logs:
	docker compose logs -f $(SERVICE)

migrate:
	docker compose exec $(SERVICE)-api dotnet ef database update

clean:
	docker compose down -v

## --- shared library packages ---

pack-shared:
	dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed
	dotnet pack src/Shared/WordBuddy.Shared.Infrastructure -o local-nuget-feed
	dotnet pack src/Shared/WordBuddy.Shared.Contracts -o local-nuget-feed

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

k8s-apply:
	kubectl apply -f k8s/namespace.yaml
	kubectl apply -f k8s/configmap.yaml -f k8s/secret.yaml
	kubectl apply -f k8s/sqlserver-statefulset.yaml -f k8s/sqlserver-service.yaml
	kubectl apply -f k8s/sqlserver-init-job.yaml
	kubectl apply -f k8s/identity-deployment.yaml -f k8s/identity-service.yaml
	kubectl apply -f k8s/content-deployment.yaml -f k8s/content-service.yaml
	kubectl apply -f k8s/quiz-deployment.yaml -f k8s/quiz-service.yaml
	kubectl apply -f k8s/progress-deployment.yaml -f k8s/progress-service.yaml
	kubectl apply -f k8s/notification-deployment.yaml -f k8s/notification-service.yaml
	kubectl apply -f k8s/ui-deployment.yaml -f k8s/ui-service.yaml
	kubectl apply -f k8s/ingress.yaml

k8s-logs:
	kubectl -n wordbuddy logs -f deploy/$(SERVICE)-api

k8s-migrate:
	kubectl -n wordbuddy exec deploy/$(SERVICE)-api -- dotnet ef database update
