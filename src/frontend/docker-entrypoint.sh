#!/bin/sh
set -eu

TOKEN='__VITE_API_URL__'
FILE='/usr/share/nginx/html/index.html'

if [ -f "$FILE" ]; then
  VALUE="${VITE_API_URL:-}"
  # Escape sed replacement characters.
  ESCAPED_VALUE=$(printf '%s' "$VALUE" | sed -e 's/[\\&]/\\&/g')
  sed -i "s|$TOKEN|$ESCAPED_VALUE|g" "$FILE"
fi
