#!/bin/bash
set -e

TAG=$(date +"%Y%m%d-%H%M%S")

docker build -t gateway-service:$TAG -f ./services/GatewayService/Dockerfile .
docker build -t tickets-service:$TAG -f ./services/TicketsService/Dockerfile .
docker build -t bonus-service:$TAG -f ./services/BonusService/Dockerfile .
docker build -t flight-service:$TAG -f ./services/FlightService/Dockerfile .
docker build -t identity-service:$TAG -f ./services/IdentityService/Dockerfile .

minikube image load gateway-service:$TAG
minikube image load tickets-service:$TAG
minikube image load bonus-service:$TAG
minikube image load flight-service:$TAG
minikube image load identity-service:$TAG

kubectl create namespace test --dry-run=client -o yaml | kubectl apply -f -

helm upgrade postgres ./charts/postgres --install --namespace test -f ./charts/postgres/values.yaml
kubectl apply -f ./charts/postgres/templates/config-map.yaml -n test

kubectl wait -n test -l app.kubernetes.io/instance=postgres pod --for=condition=Ready --timeout=120s

helm upgrade identity-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/identity-values.yaml \
  --set image.repository=identity-service \
  --set image.tag=$TAG \
  --set image.pullPolicy=IfNotPresent

helm upgrade bonus-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/bonus-values.yaml \
  --set image.repository=bonus-service --set image.tag=$TAG --set image.pullPolicy=IfNotPresent

helm upgrade flight-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/flight-values.yaml \
  --set image.repository=flight-service --set image.tag=$TAG --set image.pullPolicy=IfNotPresent

helm upgrade tickets-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/tickets-values.yaml \
  --set image.repository=tickets-service --set image.tag=$TAG --set image.pullPolicy=IfNotPresent

helm upgrade gateway-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/gateway-values.yaml \
  --set image.repository=gateway-service --set image.tag=$TAG --set image.pullPolicy=IfNotPresent

kubectl rollout restart deployment -n test

kubectl wait -n test -l app.kubernetes.io/instance=identity-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=bonus-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=flight-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=tickets-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=gateway-service pod --for=condition=Ready --timeout=180s