# Local stack: Postgres, MongoDB, Redis, and the WebApi container.
BACKEND_DIR := backend
COMPOSE := docker compose
DEV_SERVICES := \
	ambev.developerevaluation.database \
	ambev.developerevaluation.nosql \
	ambev.developerevaluation.cache \
	ambev.developerevaluation.webapi

.PHONY: dev-up

dev-up:
	cd $(BACKEND_DIR) && \
	$(COMPOSE) up -d $(DEV_SERVICES) && \
	printf 'Postgres  %s\n' "$$($(COMPOSE) port ambev.developerevaluation.database 5432)" && \
	printf 'MongoDB   %s\n' "$$($(COMPOSE) port ambev.developerevaluation.nosql 27017)" && \
	printf 'Redis     %s\n' "$$($(COMPOSE) port ambev.developerevaluation.cache 6379)" && \
	printf 'API       http://%s/swagger\n' "$$($(COMPOSE) port ambev.developerevaluation.webapi 8080)"
