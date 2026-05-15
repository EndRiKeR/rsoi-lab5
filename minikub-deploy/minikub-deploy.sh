#!/bin/bash
set -e

kubectl create namespace test --dry-run=client -o yaml | kubectl apply -f -

helm upgrade postgres ./charts/postgres --install --namespace test -f ./charts/postgres/values.yaml
kubectl apply -f ./charts/postgres/templates/config-map.yaml -n test

kubectl wait -n test -l app.kubernetes.io/instance=postgres pod --for=condition=Ready --timeout=120s

helm upgrade identity-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/identity-values.yaml \
  --set image.repository=identity-service \
  --set image.tag=latest \
  --set image.pullPolicy=IfNotPresent

helm upgrade bonus-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/bonus-values.yaml \
  --set image.repository=bonus-service --set image.tag=latest --set image.pullPolicy=IfNotPresent

helm upgrade flight-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/flight-values.yaml \
  --set image.repository=flight-service --set image.tag=latest --set image.pullPolicy=IfNotPresent

helm upgrade tickets-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/tickets-values.yaml \
  --set image.repository=tickets-service --set image.tag=latest --set image.pullPolicy=IfNotPresent

helm upgrade gateway-service ./charts/services --install --namespace test \
  -f ./charts/services/service-values/gateway-values.yaml \
  --set image.repository=gateway-service --set image.tag=latest --set image.pullPolicy=IfNotPresent \
  --set ingress.enabled=false

kubectl wait -n test -l app.kubernetes.io/instance=identity-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=bonus-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=flight-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=tickets-service pod --for=condition=Ready --timeout=180s
kubectl wait -n test -l app.kubernetes.io/instance=gateway-service pod --for=condition=Ready --timeout=180s