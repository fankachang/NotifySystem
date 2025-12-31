# Line 通知服務 - Dockerfile
# 多階段建置以優化映像大小

# 建置階段
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# 複製專案檔並還原套件（利用 Docker 快取層）
COPY src/LineNotify.Api/LineNotify.Api.csproj ./LineNotify.Api/
RUN dotnet restore ./LineNotify.Api/LineNotify.Api.csproj

# 複製所有原始碼並建置
COPY src/LineNotify.Api/ ./LineNotify.Api/
WORKDIR /src/LineNotify.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# 執行階段
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# 設定時區
ENV TZ=Asia/Taipei
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# 安裝 curl（用於健康檢查）
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# 建立非 root 使用者執行應用程式
RUN adduser --disabled-password --gecos '' appuser
USER appuser

# 複製建置產物
COPY --from=build /app/publish .

# 健康檢查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# 設定環境變數
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 暴露埠號
EXPOSE 8080

# 啟動應用程式
ENTRYPOINT ["dotnet", "LineNotify.Api.dll"]
