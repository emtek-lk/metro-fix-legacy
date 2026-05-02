# Azure Container Registry (ACR) Push Guide

## Overview
This guide explains how to build and push Docker containers to Azure Container Registry (gtkfsmregistry).

## ACR Details
- **Registry Name:** gtkfsmregistry
- **Registry URL:** gtkfsmregistry.azurecr.io
- **Username:** gtkfsmregistry
- **New Repository Names:**
  - `metrofix-api` (Backend API)
  - `metrofix-portal` (Frontend Portal)

## Setup Instructions

### Step 1: Store Credentials Securely

Create a `.env` file in the project root (never commit this to git):
```bash
export ACR_PASSWORD="PltRIT5guY8Lxc6ugJeB1afoLGd0C0T32nNEsirSHbgmiCwPWdOjJQQJ99CEACYeBjFEqg7NAAACAZCRPYCv"
export ACR_USERNAME="gtkfsmregistry"
export ACR_URL="gtkfsmregistry.azurecr.io"
```

Add to `.gitignore`:
```
.env
.env.local
```

### Step 2: Load Environment Variables
```bash
source .env
```

### Step 3: Create Script Files

Save these scripts in `deploy/scripts/`:
- `acr-login.sh`
- `build-and-push-api.sh`
- `build-and-push-portal.sh`
- `build-and-push-all.sh`

Make them executable:
```bash
chmod +x deploy/scripts/acr-login.sh
chmod +x deploy/scripts/build-and-push-api.sh
chmod +x deploy/scripts/build-and-push-portal.sh
chmod +x deploy/scripts/build-and-push-all.sh
```

## Usage

### 1. Login to ACR

**Option A: Using password as argument**
```bash
./deploy/scripts/acr-login.sh "PltRIT5guY8Lxc6ugJeB1afoLGd0C0T32nNEsirSHbgmiCwPWdOjJQQJ99CEACYeBjFEqg7NAAACAZCRPYCv"
```

**Option B: Using environment variable**
```bash
export ACR_PASSWORD="PltRIT5guY8Lxc6ugJeB1afoLGd0C0T32nNEsirSHbgmiCwPWdOjJQQJ99CEACYeBjFEqg7NAAACAZCRPYCv"
./deploy/scripts/acr-login.sh
```

**Option C: Using Azure CLI (requires az CLI installed)**
```bash
az acr login --name gtkfsmregistry
```

### 2. Build and Push Backend API

**Default (latest tag)**
```bash
./deploy/scripts/build-and-push-api.sh
```

**With specific tag**
```bash
./deploy/scripts/build-and-push-api.sh v1.0.0
```

**With custom registry**
```bash
./deploy/scripts/build-and-push-api.sh v1.0.0 gtkfsmregistry.azurecr.io
```

Result:
- Image: `gtkfsmregistry.azurecr.io/metrofix-api:v1.0.0`

### 3. Build and Push Frontend Portal

**Default (latest tag, localhost API)**
```bash
./deploy/scripts/build-and-push-portal.sh
```

**With specific tag and API URL**
```bash
./deploy/scripts/build-and-push-portal.sh v1.0.0 gtkfsmregistry.azurecr.io https://api.example.com
```

Result:
- Image: `gtkfsmregistry.azurecr.io/metrofix-portal:v1.0.0`

### 4. Build and Push All Containers (Recommended)

**Default**
```bash
./deploy/scripts/build-and-push-all.sh
```

**With specific tag**
```bash
./deploy/scripts/build-and-push-all.sh v1.0.0
```

**With custom registry and API URL**
```bash
./deploy/scripts/build-and-push-all.sh v1.0.0 gtkfsmregistry.azurecr.io https://api.example.com
```

## Complete Workflow Example

```bash
# 1. Load credentials
export ACR_PASSWORD="PltRIT5guY8Lxc6ugJeB1afoLGd0C0T32nNEsirSHbgmiCwPWdOjJQQJ99CEACYeBjFEqg7NAAACAZCRPYCv"

# 2. Login to ACR
./deploy/scripts/acr-login.sh

# 3. Build and push all containers with v1.0.0 tag
./deploy/scripts/build-and-push-all.sh v1.0.0 gtkfsmregistry.azurecr.io https://api.metrofix.com

# Output:
# ✅ All containers built and pushed successfully!
# Images Ready:
#    1️⃣  gtkfsmregistry.azurecr.io/metrofix-api:v1.0.0
#    2️⃣  gtkfsmregistry.azurecr.io/metrofix-portal:v1.0.0
```

## Using Images in Docker Compose

Update your `docker-compose.yml`:

```yaml
version: '3.9'

services:
  api:
    image: gtkfsmregistry.azurecr.io/metrofix-api:v1.0.0
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

  web-portal:
    image: gtkfsmregistry.azurecr.io/metrofix-portal:v1.0.0
    ports:
      - "80:80"
    depends_on:
      - api
```

Run with ACR authentication:
```bash
# Login first
./deploy/scripts/acr-login.sh

# Then compose up
docker-compose up
```

## Useful ACR Commands

### List all repositories
```bash
az acr repository list --name gtkfsmregistry
```

### List tags for a repository
```bash
az acr repository show-tags --name gtkfsmregistry --repository metrofix-api
```

### Delete an image
```bash
az acr repository delete --name gtkfsmregistry --image metrofix-api:latest
```

### View image details
```bash
az acr repository show --name gtkfsmregistry --image metrofix-api:v1.0.0
```

## Security Best Practices

1. **Never commit credentials** - Store in `.env` and add to `.gitignore`
2. **Use environment variables** - Don't pass passwords as command arguments in production
3. **Rotate credentials regularly** - Update ACR password periodically
4. **Use Azure CLI authentication** - For CI/CD pipelines, use `az acr login` with managed identities
5. **Enable admin account** - Already enabled in your ACR (gtkfsmregistry)

## Troubleshooting

### Authentication Failed
```bash
# Clear Docker credentials and try again
docker logout gtkfsmregistry.azurecr.io
./deploy/scripts/acr-login.sh "your-password"
```

### Docker Build Fails
```bash
# Verify Docker is running
docker ps

# Check Dockerfile paths
ls -la backend/api/Dockerfile
ls -la web-portal/Dockerfile
```

### Image Push Fails
```bash
# Check network connectivity
ping gtkfsmregistry.azurecr.io

# Verify image exists locally
docker images | grep metrofix
```

## Next Steps

1. Create CI/CD pipeline to automate builds and pushes
2. Set up image scanning for vulnerabilities
3. Configure image retention policies
4. Set up webhooks for deployment automation
5. Document deployment procedures for team
