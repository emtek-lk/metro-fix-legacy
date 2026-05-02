#!/usr/bin/env bash
set -euo pipefail

# Initializes local development database by applying all EF Core migrations.
# Usage:
#   ./database/scripts/dev-db-init.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
INFRA_PROJECT="$ROOT_DIR/backend/infrastructure/GTEK.FSM.Backend.Infrastructure.csproj"
STARTUP_PROJECT="$ROOT_DIR/backend/api/GTEK.FSM.Backend.Api.csproj"
DB_CONTEXT="GtekFsmDbContext"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

# Ensure global dotnet tools (dotnet-ef) are reachable in common setups.
export PATH="$PATH:$HOME/.dotnet/tools"
DOTNET_EF_VERSION="${DOTNET_EF_VERSION:-10.0.1}"

# Snap-based .NET installs commonly require DOTNET_ROOT for global tools.
if [[ -z "${DOTNET_ROOT:-}" && -d "/var/snap/dotnet/common/dotnet" ]]; then
  export DOTNET_ROOT="/var/snap/dotnet/common/dotnet"
fi
if [[ -z "${DOTNET_ROOT_X64:-}" && -n "${DOTNET_ROOT:-}" ]]; then
  export DOTNET_ROOT_X64="$DOTNET_ROOT"
fi

# Load .env for Docker SQL Server credentials if present
ENV_FILE="$ROOT_DIR/.env"
if [[ -f "$ENV_FILE" ]]; then
  set -o allexport
  # shellcheck source=/dev/null
  source "$ENV_FILE"
  set +o allexport
fi

# Build a local-accessible SQL Server connection string from .env values.
# This overrides the appsettings.Development.json default (Integrated Security / Server=.)
# which cannot connect to the Docker container.
SQL_HOST="${SQL_SERVER_HOST:-localhost}"
SQL_PORT="${SQL_SERVER_PORT:-12433}"
SQL_DB="${SQL_DATABASE:-GTEK_FSM_Local}"
SQL_PASS="${SA_PASSWORD:-}"

# When running from host shell, Docker compose service DNS names (e.g. sqlserver)
# are not resolvable; map to localhost for local CLI migration runs.
if [[ "$SQL_HOST" == "sqlserver" ]]; then
  SQL_HOST="localhost"
fi

if [[ -n "$SQL_PASS" ]]; then
  export Database__ConnectionString="Server=${SQL_HOST},${SQL_PORT};Database=${SQL_DB};User Id=sa;Password=${SQL_PASS};Encrypt=true;TrustServerCertificate=true;"
fi

echo "[dev-db-init] Environment: $ASPNETCORE_ENVIRONMENT"
echo "[dev-db-init] Ensuring dotnet-ef ${DOTNET_EF_VERSION} is installed..."
dotnet tool update --global dotnet-ef --version "$DOTNET_EF_VERSION" >/dev/null 2>&1 || \
  dotnet tool install --global dotnet-ef --version "$DOTNET_EF_VERSION" >/dev/null 2>&1
echo "[dev-db-init] Restoring project dependencies..."
dotnet restore "$INFRA_PROJECT"
dotnet restore "$STARTUP_PROJECT"
echo "[dev-db-init] Applying migrations..."

dotnet ef database update \
  --project "$INFRA_PROJECT" \
  --startup-project "$STARTUP_PROJECT" \
  --context "$DB_CONTEXT"

echo "[dev-db-init] Database initialized and up to date."
