FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GTEK.FSM.slnx", "./"]
COPY ["web-portal/GTEK.FSM.WebPortal.csproj", "web-portal/"]
COPY ["shared/contracts/GTEK.FSM.Shared.Contracts.csproj", "shared/contracts/"]
RUN dotnet restore "web-portal/GTEK.FSM.WebPortal.csproj"

COPY . .
WORKDIR /src/web-portal
RUN dotnet publish "GTEK.FSM.WebPortal.csproj" -c Release -o /app/publish

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html
COPY web-portal/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot .

# Entrypoint writes runtime config (config.json) from environment variables, then starts nginx.
COPY web-portal/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
