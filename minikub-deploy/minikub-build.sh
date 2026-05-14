#!/bin/bash
set -e

docker build -t gateway-service:latest -f ./services/GatewayService/Dockerfile .
docker build -t tickets-service:latest -f ./services/TicketsService/Dockerfile .
docker build -t bonus-service:latest -f ./services/BonusService/Dockerfile .
docker build -t flight-service:latest -f ./services/FlightService/Dockerfile .
docker build -t identity-service:latest -f ./services/IdentityService/Dockerfile .

minikube image load gateway-service:latest
minikube image load tickets-service:latest
minikube image load bonus-service:latest
minikube image load flight-service:latest
minikube image load identity-service:latest

kubectl create namespace test --dry-run=client -o yaml | kubectl apply -f -

helm upgrade postgres ./charts/postgres --install --namespace test -f ./charts/postgres/values.yaml
kubectl apply -f ./charts/postgres/templates/config-map.yaml -n test

kubectl wait -n test -l app.kubernetes.io/instance=postgres pod --for=condition=Ready --timeout=120s