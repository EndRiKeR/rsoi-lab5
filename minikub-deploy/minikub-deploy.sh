#!/bin/bash
set -e

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

kubectl port-forward -n test svc/gateway-service 8080:8080 &
PF_GATEWAY_PID=$!
sleep 5
curl --retry 10 --retry-delay 2 http://localhost:8080/manage/health
newman run v1/postman/collection.json -e v1/postman/environment.json --delay-request 100
kill $PF_GATEWAY_PID