#!/bin/bash
set -e

#minikube stop
#minikube delete --all
minikube start --cpus=4 --memory=6144 --driver=docker
minikube addons enable ingress

minikube tunnel > /dev/null 2>&1 &
TUNNEL_PID=$!
echo "Туннель запущен с PID $TUNNEL_PID"

sleep 5

echo "=== Сборка, загрузка, развертывание образов ==="
./minikube-deploy/minikube-build.sh

sleep 10

for i in $(seq 1 10); do
  if curl -s http://localhost/manage/health > /dev/null; then
    echo "Gateway доступен через Ingress!"
    break
  fi
  echo "Ожидание готовности Ingress ($i/10)..."
  sleep 5
done

echo "=== Готово! Теперь можно запускать тесты Newman ==="

./minikube-deploy/run-tests.sh

echo "Туннель продолжает работать (PID $TUNNEL_PID). Для остановки выполните: kill $TUNNEL_PID"