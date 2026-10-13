# ==========================================
# Build Stage
# ==========================================
FROM oven/bun:1-alpine AS build
WORKDIR /app

# 패키지 매니페스트 및 락파일 복사
COPY package.json bun.lock ./

# 의존성 설치
RUN bun install --frozen-lockfile

# 소스코드 전체 복사 및 SvelteKit 빌드
COPY . .
RUN bun run build

# ==========================================
# Runtime Stage
# ==========================================
FROM oven/bun:1-alpine AS runtime
WORKDIR /app

# 프로덕션 실행에 필요한 산출물 복사
COPY --from=build /app/package.json ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/build ./build

# 환경 변수 설정
ENV NODE_ENV=production
ENV PORT=3000
ENV HOST=0.0.0.0

EXPOSE 3000

ENTRYPOINT ["bun", "run", "build/index.js"]
