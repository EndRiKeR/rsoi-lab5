#!/bin/bash

#minikube stop
#minikube delete --all

minikube start --driver=docker --cpus=4 --memory=6144

minikube addons enable ingress

D:/myProgects/repLab/rsoi-lab5/minikube-deploy/minikube-build.sh

kubectl get pods -n test