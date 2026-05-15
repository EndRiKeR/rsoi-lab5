#!/bin/bash
set -e

docker build --no-cache -t gateway-service:latest -f ./services/GatewayService/Dockerfile .
docker build --no-cache -t tickets-service:latest -f ./services/TicketsService/Dockerfile .
docker build --no-cache -t bonus-service:latest -f ./services/BonusService/Dockerfile .
docker build --no-cache -t flight-service:latest -f ./services/FlightService/Dockerfile .
docker build --no-cache -t identity-service:latest -f ./services/IdentityService/Dockerfile .

minikube image load gateway-service:latest
minikube image load tickets-service:latest
minikube image load bonus-service:latest
minikube image load flight-service:latest
minikube image load identity-service:latest