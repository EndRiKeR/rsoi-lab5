#!/bin/bash
set -e

COLLECTION="./postman/classic_fail.json"
ENVIRONMENT="./postman/environment.json"

newman run "$COLLECTION" -e "$ENVIRONMENT" --delay-request 100 --reporters cli