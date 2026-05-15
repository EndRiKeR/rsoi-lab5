kubectl port-forward -n test svc/gateway-service 8080:8080 &
PF_GATEWAY_PID=$!

kubectl port-forward -n test svc/identity-service 8090:8090 &
PF_IDENTITY_PID=$!