# Local stack: Postgres, MongoDB, Redis, and the WebApi container.
COMPOSE := docker compose
DEV_SERVICES := \
	ambev.developerevaluation.database \
	ambev.developerevaluation.nosql \
	ambev.developerevaluation.cache \
	ambev.developerevaluation.webapi

.PHONY: dev-up debug-up

dev-up:
	$(COMPOSE) up -d $(DEV_SERVICES) && \
	printf 'Postgres  %s\n' "$$($(COMPOSE) port ambev.developerevaluation.database 5432)" && \
	printf 'MongoDB   %s\n' "$$($(COMPOSE) port ambev.developerevaluation.nosql 27017)" && \
	printf 'Redis     %s\n' "$$($(COMPOSE) port ambev.developerevaluation.cache 6379)" && \
	printf 'API       http://%s/swagger\n' "$$($(COMPOSE) port ambev.developerevaluation.webapi 8080)"

# The same stack with the API built in Debug: /api/diagnostics and the StepTrace buffer exist. Replaces the API
# container of dev-up (same name and port).
DEBUG_COMPOSE := $(COMPOSE) -f docker-compose.yml -f docker-compose.debug.yml

debug-up:
	$(DEBUG_COMPOSE) up -d --build $(DEV_SERVICES) && \
	printf 'Postgres  %s\n' "$$($(DEBUG_COMPOSE) port ambev.developerevaluation.database 5432)" && \
	printf 'MongoDB   %s\n' "$$($(DEBUG_COMPOSE) port ambev.developerevaluation.nosql 27017)" && \
	printf 'Redis     %s\n' "$$($(DEBUG_COMPOSE) port ambev.developerevaluation.cache 6379)" && \
	printf 'API       http://%s/swagger (Debug)\n' "$$($(DEBUG_COMPOSE) port ambev.developerevaluation.webapi 8080)"
