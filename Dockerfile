# =====================================================
# ЭТАП 1: СБОРКА
# =====================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только csproj для кэширования restore
COPY ["Backend/Backend.csproj", "Backend/"]
COPY ["DataBase/DataBase.csproj", "DataBase/"]

# Восстанавливаем пакеты (использует стандартный путь ~/.nuget)
RUN dotnet restore "Backend/Backend.csproj"

# Копируем исходный код
COPY . .

# 🔥 Очищаем кэш и публикуем
RUN dotnet nuget locals all --clear \
    && dotnet publish "Backend/Backend.csproj" -c Release -o /app/publish

# =====================================================
# ЭТАП 2: ЗАПУСК
# =====================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Priazov.dll"]