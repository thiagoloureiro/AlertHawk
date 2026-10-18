<p align="center">
  <img src="logo.png" alt="AlertHawk logo" width="160" />
</p>

<h1 align="center">AlertHawk</h1>

<p align="center">
  <strong>Self-hosted monitoring for APIs, websites, servers, Kubernetes, and more</strong>
</p>

<p align="center">
  <a href="https://alerthawk.net">Live demo</a> ·
  <a href="https://github.com/thiagoloureiro/AlertHawk/tree/main/AlertHawk.Docs">Docs</a> ·
  <a href="https://artifacthub.io/packages/search?repo=alerthawk">Helm charts</a> ·
  <a href="https://github.com/thiagoloureiro/AlertHawk.Chart">Chart repo</a>
</p>

<p align="center">
  <a href="https://github.com/thiagoloureiro/AlertHawk/blob/main/LICENSE"><img src="https://img.shields.io/github/license/thiagoloureiro/AlertHawk?style=flat-square" alt="License" /></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10" /></a>
  <a href="https://alerthawk.net"><img src="https://img.shields.io/badge/demo-alerthawk.net-0A7EA4?style=flat-square" alt="Live demo" /></a>
  <a href="https://github.com/thiagoloureiro/AlertHawk/stargazers"><img src="https://img.shields.io/github/stars/thiagoloureiro/AlertHawk?style=flat-square" alt="Stars" /></a>
  <a href="https://github.com/thiagoloureiro/AlertHawk/network/members"><img src="https://img.shields.io/github/forks/thiagoloureiro/AlertHawk?style=flat-square" alt="Forks" /></a>
  <a href="https://github.com/thiagoloureiro/AlertHawk/issues"><img src="https://img.shields.io/github/issues/thiagoloureiro/AlertHawk?style=flat-square" alt="Issues" /></a>
  <a href="https://github.com/thiagoloureiro/AlertHawk/commits/main"><img src="https://img.shields.io/github/last-commit/thiagoloureiro/AlertHawk?style=flat-square" alt="Last commit" /></a>
  <a href="https://app.codacy.com/gh/thiagoloureiro/AlertHawk/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade"><img src="https://app.codacy.com/project/badge/Grade/20da64ba9f9d48abb0733064d2491da4" alt="Codacy grade" /></a>
  <a href="https://ko-fi.com/thiagoloureiro"><img src="https://img.shields.io/badge/Ko--fi-Support-ff5e5b?style=flat-square&logo=ko-fi&logoColor=white" alt="Ko-fi" /></a>
</p>

---

## Overview

**AlertHawk** is a self-hosted monitoring platform that gives you real-time visibility into the health and performance of your stack. Monitor WebAPIs, websites, TCP endpoints, Kubernetes clusters, and more — with alerts delivered through the channels you already use.

**Try it now:** [https://alerthawk.net](https://alerthawk.net) — create an account or sign in with a personal Microsoft account.

## Features

- **Versatile monitoring** — HTTP/HTTPS, TCP, Kubernetes nodes/pods, and more
- **Microservices architecture** — modular services for availability and scale
- **Cloud & on-prem ready** — run anywhere you can host containers
- **Rich notifications** — email, Slack, Teams, Telegram, webhooks, and more
- **Metrics & FinOps** — cluster metrics plus Azure cost and resource insights
- **Kubernetes-native** — Helm charts on [Artifact Hub](https://artifacthub.io/packages/search?repo=alerthawk)

## Architecture

| Component | Description |
|-----------|-------------|
| **Authentication** | Identity and access (e.g. Microsoft Account / Azure AD) |
| **Monitoring** | Runs monitors (HTTP, TCP, K8s, etc.) and manages alerts |
| **Notification** | Delivers alerts to configured channels |
| **Metrics Agent** | Collects metrics from your environments |
| **Metrics API** | Stores and queries metrics data |
| **FinOps** | Azure cost/resource analysis with optional AI recommendations |
| **UI** | Web dashboard |

Architecture diagram: [Diagrams/AlertHawk.drawio](Diagrams/AlertHawk.drawio)

## Screenshots

<p align="center">
  <img src="https://github.com/user-attachments/assets/1ce3f2a8-e852-47c2-a5c5-7cb6f8da8214" alt="AlertHawk dashboard" width="800" />
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/555b3173-0af9-4cc3-922b-29757385960a" alt="AlertHawk monitors" width="800" />
  <img src="https://github.com/user-attachments/assets/cc9700ea-cdda-417f-9bdd-4bb7c91fd1a2" alt="AlertHawk details" width="800" />
</p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/b9544f62-2a19-4db2-a439-efcb6a0a409d" alt="AlertHawk alerts" width="800" />
  <img src="https://github.com/user-attachments/assets/1bf92c6b-8afb-4629-9c97-6bd17b6edc79" alt="AlertHawk metrics" width="800" />
</p>

<p align="center">
  <img src="https://github.com/thiagoloureiro/AlertHawk/assets/29433794/357d0ce6-fc87-4998-b7bc-6c154867264a" alt="AlertHawk mobile" width="280" />
</p>

## Quick start (Helm)

```bash
helm repo add alerthawk https://thiagoloureiro.github.io/AlertHawk.Chart/
helm repo update

helm show values alerthawk/alerthawk > my-values.yaml
# Edit my-values.yaml with your connection strings and secrets

helm install alerthawk alerthawk/alerthawk -f my-values.yaml
```

More details: [Helm docs](AlertHawk.Docs/helm/index.md) · [AlertHawk.Chart](https://github.com/thiagoloureiro/AlertHawk.Chart)

## Docker images & CI

Images are published to Docker Hub under [`thiagoguaru`](https://hub.docker.com/u/thiagoguaru).

### Notification

[![Docker Image Size](https://img.shields.io/docker/image-size/thiagoguaru/alerthawk.notification/latest?style=flat-square&label=image%20size)](https://hub.docker.com/r/thiagoguaru/alerthawk.notification)
[![Docker Pulls](https://img.shields.io/docker/pulls/thiagoguaru/alerthawk.notification?style=flat-square)](https://hub.docker.com/r/thiagoguaru/alerthawk.notification)
[![Build status](https://thiagoguaru.visualstudio.com/AlertHawk/_apis/build/status/AlertHawk.Notification%20-%20Docker)](https://thiagoguaru.visualstudio.com/AlertHawk/_build/latest?definitionId=10)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/thiagoguaru/alerthawk/9?style=flat-square)
![Azure DevOps tests](https://img.shields.io/azure-devops/tests/thiagoguaru/alerthawk/9?style=flat-square)

### Authentication

[![Docker Image Size](https://img.shields.io/docker/image-size/thiagoguaru/alerthawk.authentication/latest?style=flat-square&label=image%20size)](https://hub.docker.com/r/thiagoguaru/alerthawk.authentication)
[![Docker Pulls](https://img.shields.io/docker/pulls/thiagoguaru/alerthawk.authentication?style=flat-square)](https://hub.docker.com/r/thiagoguaru/alerthawk.authentication)
[![Build status](https://thiagoguaru.visualstudio.com/AlertHawk/_apis/build/status/AlertHawk.Authentication%20-%20Docker)](https://thiagoguaru.visualstudio.com/AlertHawk/_build/latest?definitionId=14)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/thiagoguaru/alerthawk/14?style=flat-square)
![Azure DevOps tests](https://img.shields.io/azure-devops/tests/thiagoguaru/alerthawk/14?style=flat-square)

### Monitoring

[![Docker Image Size](https://img.shields.io/docker/image-size/thiagoguaru/alerthawk.monitoring/latest?style=flat-square&label=image%20size)](https://hub.docker.com/r/thiagoguaru/alerthawk.monitoring)
[![Docker Pulls](https://img.shields.io/docker/pulls/thiagoguaru/alerthawk.monitoring?style=flat-square)](https://hub.docker.com/r/thiagoguaru/alerthawk.monitoring)
[![Build status](https://thiagoguaru.visualstudio.com/AlertHawk/_apis/build/status/AlertHawk.Monitoring%20-%20Docker)](https://thiagoguaru.visualstudio.com/AlertHawk/_build/latest?definitionId=11)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/thiagoguaru/alerthawk/12?style=flat-square)
![Azure DevOps tests](https://img.shields.io/azure-devops/tests/thiagoguaru/alerthawk/12?style=flat-square)

### Metrics

[![Docker Image Size (API)](https://img.shields.io/docker/image-size/thiagoguaru/alerthawk.metrics.api/latest?style=flat-square&label=api%20image)](https://hub.docker.com/r/thiagoguaru/alerthawk.metrics.api)
[![Docker Pulls (API)](https://img.shields.io/docker/pulls/thiagoguaru/alerthawk.metrics.api?style=flat-square&label=api%20pulls)](https://hub.docker.com/r/thiagoguaru/alerthawk.metrics.api)
[![Docker Image Size (Agent)](https://img.shields.io/docker/image-size/thiagoguaru/alerthawk.metrics/latest?style=flat-square&label=agent%20image)](https://hub.docker.com/r/thiagoguaru/alerthawk.metrics)
[![Docker Pulls (Agent)](https://img.shields.io/docker/pulls/thiagoguaru/alerthawk.metrics?style=flat-square&label=agent%20pulls)](https://hub.docker.com/r/thiagoguaru/alerthawk.metrics)
[![Build status](https://dev.azure.com/thiagoguaru/AlertHawk/_apis/build/status/AlertHawk.Monitoring/AlertHawk.Metrics%20-%20Tests)](https://dev.azure.com/thiagoguaru/AlertHawk/_build/latest?definitionId=54)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/thiagoguaru/alerthawk/54?style=flat-square)
![Azure DevOps tests](https://img.shields.io/azure-devops/tests/thiagoguaru/alerthawk/54?style=flat-square)

### FinOps

[![Docker Image Size](https://img.shields.io/docker/image-size/thiagoguaru/alerthawk.finops.api/latest?style=flat-square&label=image%20size)](https://hub.docker.com/r/thiagoguaru/alerthawk.finops.api)
[![Docker Pulls](https://img.shields.io/docker/pulls/thiagoguaru/alerthawk.finops.api?style=flat-square)](https://hub.docker.com/r/thiagoguaru/alerthawk.finops.api)
[![Build status](https://thiagoguaru.visualstudio.com/AlertHawk/_apis/build/status/AlertHawk.Monitoring%20-%20Docker)](https://thiagoguaru.visualstudio.com/AlertHawk/_build/latest?definitionId=65)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/thiagoguaru/alerthawk/66?style=flat-square)
![Azure DevOps tests](https://img.shields.io/azure-devops/tests/thiagoguaru/alerthawk/66?style=flat-square)

## Documentation

| Topic | Link |
|-------|------|
| Introduction | [AlertHawk.Docs/intro.md](AlertHawk.Docs/intro.md) |
| Monitoring | [AlertHawk.Docs/monitoring](AlertHawk.Docs/monitoring) |
| Notification | [AlertHawk.Docs/notification](AlertHawk.Docs/notification) |
| Authentication | [AlertHawk.Docs/authentication](AlertHawk.Docs/authentication) |
| Metrics API / Agent | [AlertHawk.Docs/metrics-api](AlertHawk.Docs/metrics-api) · [metrics-agent](AlertHawk.Docs/metrics-agent) |
| FinOps | [AlertHawk.Docs/finops](AlertHawk.Docs/finops) |
| Helm | [AlertHawk.Docs/helm](AlertHawk.Docs/helm) |

## Contributors

Thanks to everyone who has contributed:

| [![BulandaK](https://avatars.githubusercontent.com/BulandaK?s=100)](https://github.com/BulandaK) | [![altughan09](https://avatars.githubusercontent.com/altughan09?s=100)](https://github.com/altughan09) | [![guilhermedclima](https://avatars.githubusercontent.com/guilhermedclima?s=100)](https://github.com/guilhermedclima) | [![pvinicius](https://avatars.githubusercontent.com/pvinicius?s=100)](https://github.com/pvinicius) |
|:---:|:---:|:---:|:---:|
| [BulandaK](https://github.com/BulandaK) | [altughan09](https://github.com/altughan09) | [guilhermedclima](https://github.com/guilhermedclima) | [pvinicius](https://github.com/pvinicius) |

Full list: [contributors](https://github.com/thiagoloureiro/AlertHawk/graphs/contributors)

## License

This project is licensed under the [MIT License](LICENSE).

Support the project on [Ko-fi](https://ko-fi.com/thiagoloureiro).
