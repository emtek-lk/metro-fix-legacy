#!/bin/bash

# Build and Push All Containers to ACR
# Usage: ./build-and-push-all.sh [tag] [registry-url] [api-base-url]
set -e

IMAGE_TAG="${1:-latest}"
ACR_URL="${2:-gtkfsmregistry.azurecr.io}"
API_BASE_URL="${3:-http://localhost:8080}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=========================================="
echo "🚀 Building and Pushing All Containers"
echo "=========================================="
echo "Tag: $IMAGE_TAG"
echo "Registry: $ACR_URL"
echo "API Base URL: $API_BASE_URL"
echo ""

echo "▶️  Step 1/2: Building and pushing Backend API..."
"$SCRIPT_DIR/build-and-push-api.sh" "$IMAGE_TAG" "$ACR_URL"
if [ $? -ne 0 ]; then
    echo "❌ Failed to build and push Backend API"
    exit 1
fi

echo ""
sleep 1

echo "▶️  Step 2/2: Building and pushing Frontend Portal..."
"$SCRIPT_DIR/build-and-push-portal.sh" "$IMAGE_TAG" "$ACR_URL" "$API_BASE_URL"
if [ $? -ne 0 ]; then
    echo "❌ Failed to build and push Frontend Portal"
    exit 1
fi

echo ""
echo "=========================================="
echo "✅ All containers built and pushed successfully!"
echo "=========================================="
echo ""
echo "📋 Images Ready:"
echo "   1️⃣  ${ACR_URL}/metrofix-api:${IMAGE_TAG}"
echo "   2️⃣  ${ACR_URL}/metrofix-portal:${IMAGE_TAG}"
