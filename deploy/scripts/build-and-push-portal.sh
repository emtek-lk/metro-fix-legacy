#!/bin/bash

# Build and Push Frontend Portal to ACR
# Usage: ./build-and-push-portal.sh [tag] [registry-url] [api-base-url]
set -e

ACR_URL="${2:-gtkfsmregistry.azurecr.io}"
REPO_NAME="metrofix-portal"
IMAGE_TAG="${1:-latest}"
API_BASE_URL="${3:-http://localhost:8080}"
FULL_IMAGE_NAME="${ACR_URL}/${REPO_NAME}:${IMAGE_TAG}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"

echo "🚀 Building and Pushing Frontend Portal"
echo "   Registry: $ACR_URL"
echo "   Repository: $REPO_NAME"
echo "   Image: $FULL_IMAGE_NAME"
echo "   API Base URL: $API_BASE_URL"
echo ""

cd "$PROJECT_ROOT"

echo "🏗️  Building Docker image..."
docker build \
    --platform linux/amd64 \
    -f web-portal/Dockerfile \
    -t "$FULL_IMAGE_NAME" \
    --label "git.commit=$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')" \
    --label "build.date=$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
    .

if [ $? -ne 0 ]; then
    echo "❌ Docker build failed"
    exit 1
fi

echo "✅ Docker image built successfully"
echo ""
echo "📤 Pushing image to ACR..."
docker push "$FULL_IMAGE_NAME"

if [ $? -ne 0 ]; then
    echo "❌ Docker push failed"
    exit 1
fi

echo "✅ Image pushed successfully to $FULL_IMAGE_NAME"
