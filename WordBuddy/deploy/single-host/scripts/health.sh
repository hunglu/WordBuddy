#!/bin/sh
# Checks /health/ready on the five APIs from inside the Compose network.
# Prints one line per API. Exit code 1 if any API is not ready.
set -u
network="${COMPOSE_NETWORK:-wordbuddy-prod}"
status=0

for api in identity-api content-api quiz-api progress-api notification-api; do
  code="$(docker run --rm --network "$network" curlimages/curl:8.11.1 \
    -s -o /dev/null -w '%{http_code}' --max-time 10 "http://$api:8080/health/ready" || true)"
  if [ "$code" = "200" ]; then
    echo "OK    $api"
  else
    echo "FAIL  $api (HTTP ${code:-none})"
    status=1
  fi
done
exit "$status"
