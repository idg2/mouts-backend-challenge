#!/bin/sh
# Work item: TASK-093 (FEAT-020)
# Runs before the image's template step: stops the container when a variable is missing, instead of serving empty
# values, then writes /config.json.
set -eu

for name in API_UPSTREAM UI_LOGIN_EMAIL UI_LOGIN_PASSWORD; do
    eval "value=\${$name:-}"
    if [ -z "$value" ]; then
        echo "validation-ui: $name is not set" >&2
        exit 1
    fi
done

case "$UI_LOGIN_EMAIL$UI_LOGIN_PASSWORD" in
    *\"*|*\\*)
        echo 'validation-ui: UI_LOGIN_EMAIL and UI_LOGIN_PASSWORD must not contain " or \' >&2
        exit 1
        ;;
esac

envsubst '${UI_LOGIN_EMAIL} ${UI_LOGIN_PASSWORD}' < /etc/validation-ui/config.json.template > /usr/share/nginx/html/config.json
