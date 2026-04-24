FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GTEK.FSM.slnx", "./"]
COPY ["backend/api/GTEK.FSM.Backend.Api.csproj", "backend/api/"]
COPY ["backend/application/GTEK.FSM.Backend.Application.csproj", "backend/application/"]
COPY ["backend/domain/GTEK.FSM.Backend.Domain.csproj", "backend/domain/"]
COPY ["backend/infrastructure/GTEK.FSM.Backend.Infrastructure.csproj", "backend/infrastructure/"]
COPY ["shared/contracts/GTEK.FSM.Shared.Contracts.csproj", "shared/contracts/"]
RUN dotnet restore "backend/api/GTEK.FSM.Backend.Api.csproj"

COPY . .
WORKDIR /src/backend/api

# /p:EnableDefaultEmbeddedResourceItems=false — suppresses MSB3552 (EF Design resx glob)
# /p:EnableDefaultCompileItems=false — suppresses CS2021 (EF Design cs glob)
# Infrastructure.csproj has explicit <Compile Include="**/*.cs" /> to compensate,
# and uses forward-slash ProjectReferences for Linux compatibility.
RUN dotnet publish "GTEK.FSM.Backend.Api.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:EnableDefaultEmbeddedResourceItems=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GTEK.FSM.Backend.Api.dll"]
