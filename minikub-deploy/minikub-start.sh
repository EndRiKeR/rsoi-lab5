#!/bin/bash
set -e

kubectl port-forward -n test svc/gateway-service 8080:8080 &
PF_GATEWAY_PID=$!
sleep 5
curl --retry 10 --retry-delay 2 http://localhost:8080/manage/health
newman run v1/postman/collection.json -e v1/postman/environment.json --delay-request 100
kill $PF_GATEWAY_PID