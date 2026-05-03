#!/bin/sh
set -e

# Default API base URL when not provided via environment (local docker default)
: ${API_BASE_URL:=http://localhost:8080}

# Ensure config.json contains the runtime API URL for the frontend
cat > /usr/share/nginx/html/config.json <<EOF
{"ApiBaseUrl":"${API_BASE_URL}"}
EOF

# Start nginx in foreground
exec nginx -g 'daemon off;'
