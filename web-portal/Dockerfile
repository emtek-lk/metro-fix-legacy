FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG WEB_API_BASE_URL=http://localhost:5000

COPY ["GTEK.FSM.slnx", "./"]
COPY ["web-portal/GTEK.FSM.WebPortal.csproj", "web-portal/"]
COPY ["shared/contracts/GTEK.FSM.Shared.Contracts.csproj", "shared/contracts/"]
RUN dotnet restore "web-portal/GTEK.FSM.WebPortal.csproj"

COPY . .
WORKDIR /src/web-portal
RUN dotnet publish "GTEK.FSM.WebPortal.csproj" -c Release -o /app/publish
RUN sed -i "s|\"BaseUrl\": \".*\"|\"BaseUrl\": \"${WEB_API_BASE_URL}\"|g" /app/publish/wwwroot/appsettings.json

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html
COPY web-portal/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot .
EXPOSE 80
