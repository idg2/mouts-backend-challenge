# Work item: TASK-093 (FEAT-020)
# Builds the Angular app and serves it with nginx, which proxies /api/ to API_UPSTREAM and writes /config.json
# from UI_LOGIN_EMAIL and UI_LOGIN_PASSWORD at startup.
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build

FROM nginx:stable-alpine
# Exposes the container's DNS resolver to the template as NGINX_LOCAL_RESOLVERS.
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
COPY --from=build /app/dist/validation-ui/browser /usr/share/nginx/html
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY nginx/config.json.template /etc/validation-ui/config.json.template
COPY nginx/10-validation-ui.sh /docker-entrypoint.d/10-validation-ui.sh
RUN chmod +x /docker-entrypoint.d/10-validation-ui.sh
EXPOSE 8080
