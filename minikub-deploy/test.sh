#!/bin/bash
set -e

echo "=== 1. Проброс порта PostgreSQL ==="
kubectl port-forward -n test svc/postgres 5432:5432 &
PF_PID=$!
sleep 3

echo ""
echo "=== 2. Установка строки подключения ==="
export DOCKER_CONNECT_STRING="Host=localhost;Port=5432;Database=identity;Username=postgres;Password=postgres"

echo ""
echo "=== 3. Добавление миграции и обновление базы ==="
cd ./services/IdentityService

# Добавляем миграцию (если папка Migrations уже есть, можно пропустить, но команда перезапишет)
dotnet ef migrations add InitialCreate --context ApplicationDbContext

# Применяем миграции к базе
dotnet ef database update --context ApplicationDbContext

cd ../../

echo ""
echo "=== 4. Исправление Program.cs (добавляем openid scope) ==="
PROGRAM_FILE="./services/IdentityService/Program.cs"
sed -i 's|// OpenIddictConstants.Permissions.Scopes.OpenId,|OpenIddictConstants.Permissions.Scopes.OpenId,|' "$PROGRAM_FILE"

echo ""
echo "=== 5. Пересборка образа IdentityService ==="
docker build --no-cache -t identity-service:latest -f ./services/IdentityService/Dockerfile .
minikube image load identity-service:latest

echo ""
echo "=== 6. Перезапуск деплоймента ==="
kubectl rollout restart deployment identity-service -n test

echo ""
echo "=== 7. Ожидание готовности ==="
kubectl wait -n test -l app.kubernetes.io/instance=identity-service pod --for=condition=Ready --timeout=180s

echo ""
echo "=== 8. Остановка port-forward ==="
kill $PF_PID 2>/dev/null || true

echo ""
echo "Готово! Identity Service должен работать."