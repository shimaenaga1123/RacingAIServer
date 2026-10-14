# ==========================================
# Build Stage
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 캐시 최적화를 위한 csproj 사전 복사
COPY src/Racing.Contracts/Racing.Contracts.csproj src/Racing.Contracts/
COPY src/Racing.Domain/Racing.Domain.csproj src/Racing.Domain/
COPY src/Racing.Simulation/Racing.Simulation.csproj src/Racing.Simulation/
COPY src/Racing.Application/Racing.Application.csproj src/Racing.Application/
COPY src/Racing.Infrastructure/Racing.Infrastructure.csproj src/Racing.Infrastructure/
COPY src/Racing.Api/Racing.Api.csproj src/Racing.Api/

# 의존성 복원
RUN dotnet restore src/Racing.Api/Racing.Api.csproj

# 전체 소스코드 복사 및 빌드/게시
COPY src/ src/
COPY tracks/ /app/tracks/

WORKDIR /src/src/Racing.Api
RUN dotnet publish Racing.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Runtime Stage
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# 기본 의존 라이브러리 및 디렉터리 준비
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/tracks /app/replays

# 빌드 산출물 복사
COPY --from=build /app/publish .
COPY --from=build /app/tracks ./tracks

# 환경 변수 설정 (REST, SignalR, gRPC 공용 HTTPS 포트)
ENV ASPNETCORE_URLS=https://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 5000

ENTRYPOINT ["dotnet", "Racing.Api.dll"]
