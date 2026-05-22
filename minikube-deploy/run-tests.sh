#!/bin/bash
set -e

COLLECTION="./postman/classic_fail.json"
ENVIRONMENT="./postman/environment.json"

NAMESPACE="test"

echo "=== 1. Основной сценарий (все сервисы работают) ==="
newman run "$COLLECTION" -e "$ENVIRONMENT" \
      --folder "Gateway API" \
      --delay-request 100 \
      --reporters cli \
      --export-environment "$ENVIRONMENT"

echo ""
echo "=== 2. Отключаем Bonus Service ==="
kubectl scale deployment bonus-service -n "$NAMESPACE" --replicas=0
sleep 10

echo "=== 3. failover step1 (Bonus выключен) ==="
newman run "$COLLECTION" -e "$ENVIRONMENT" \
      --folder "step1 – Turn off Bonus Service" \
      --delay-request 100 \
      --reporters cli \
      --export-environment "$ENVIRONMENT"

echo ""
echo "=== 4. Включаем Bonus Service ==="
kubectl scale deployment bonus-service -n "$NAMESPACE" --replicas=1
kubectl wait --for=condition=Available deployment/bonus-service -n "$NAMESPACE" --timeout=300s

echo "=== 5. failover step2 (Bonus включен) ==="
newman run "$COLLECTION" -e "$ENVIRONMENT" \
      --folder "step2 – Turn on Bonus Service" \
      --delay-request 100 \
      --reporters cli \
      --export-environment "$ENVIRONMENT"

echo ""
echo "=== 6. Снова отключаем Bonus Service ==="
kubectl scale deployment bonus-service -n "$NAMESPACE" --replicas=0
sleep 10

echo "=== 7. failover step3 (возврат при выключенном Bonus) ==="
newman run "$COLLECTION" -e "$ENVIRONMENT" \
      --folder "step3 – Turn off Bonus Service (return)" \
      --delay-request 100 \
      --reporters cli \
      --export-environment "$ENVIRONMENT"

echo ""
echo "=== 8. Включаем Bonus Service обратно ==="
kubectl scale deployment bonus-service -n "$NAMESPACE" --replicas=1
kubectl wait --for=condition=Available deployment/bonus-service -n "$NAMESPACE" --timeout=300s
sleep 20

echo "=== 9. failover step4 (проверка после возврата) ==="
newman run "$COLLECTION" -e "$ENVIRONMENT" \
      --folder "step4 – Turn on Bonus Service (check return)" \
      --delay-request 100 \
      --reporters cli \
      --export-environment "$ENVIRONMENT"

echo ""
echo "=== Все тесты завершены! ==="