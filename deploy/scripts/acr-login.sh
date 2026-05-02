#!/bin/bash

# Azure Container Registry Login Script
set -e

ACR_NAME="gtkfsmregistry"
ACR_URL="${ACR_NAME}.azurecr.io"
ACR_USERNAME="gtkfsmregistry"
ACR_PASSWORD="${ACR_PASSWORD:-}"

if [ $# -eq 1 ]; then
    ACR_PASSWORD="$1"
fi

if [ -z "$ACR_PASSWORD" ]; then
    echo "❌ Error: ACR_PASSWORD not provided"
    echo ""
    echo "Usage:"
    echo "  1. Set environment variable: export ACR_PASSWORD='your-password'"
    echo "  2. Or pass as argument: $0 'your-password'"
    exit 1
fi

echo "🔐 Logging in to Azure Container Registry: $ACR_URL"
echo "$ACR_PASSWORD" | docker login "$ACR_URL" -u "$ACR_USERNAME" --password-stdin

if [ $? -eq 0 ]; then
    echo "✅ Successfully logged in to $ACR_URL"
else
    echo "❌ Failed to login to $ACR_URL"
    exit 1
fi
