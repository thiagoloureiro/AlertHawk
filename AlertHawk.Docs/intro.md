# Introduction

**AlertHawk** is a self-hosted monitoring tool that gives you real-time visibility into the health and performance of your applications. It is built as a set of microservices that work together.

## What is AlertHawk?

AlertHawk helps you monitor:

- **WebAPIs** and websites
- **Servers** and infrastructure
- **Databases** and other components
- **Kubernetes**: node CPU and memory, namespaces, pods, container metrics, Kubernetes events, and pod logs

You get real-time insights and alerts so you can react quickly to issues.

## Architecture Overview

A high-level architecture diagram is available on GitHub. It shows how the services (Monitoring, Notification, Authentication, Metrics API, Metrics Agent) connect to databases, message queues, and external systems.

**[View architecture diagram (draw.io)](https://github.com/thiagoloureiro/AlertHawk/blob/main/Diagrams/AlertHawk.drawio)** — GitHub will open and render the diagram in the browser.

::: tip Inline image
To show the diagram directly in this docs site, export it from [draw.io](https://app.diagrams.net/) or diagrams.net as PNG or SVG, save it in `AlertHawk.Docs/public/` (e.g. `architecture.png`), then add: `![Architecture](/architecture.png)` in this section.
:::

AlertHawk is composed of these main parts:

| Component | Description |
|-----------|-------------|
| **Authentication** | User identity and access (e.g. Microsoft Account login) |
| **Metrics Agent** | Collects and sends metrics from your environments |
| **Metrics API** | API to store and query metrics data |
| **Monitoring** | Runs monitors (HTTP, TCP, K8s, etc.) and manages alerts |
| **Notification** | Sends alerts via email, Slack, Teams, Telegram, webhooks, and more |

## Documentation Sections

- **[Authentication](/authentication/)** — Setup and usage of the authentication service
- **[Metrics Agent](/metrics-agent/)** — Installing and configuring the metrics agent
- **[Metrics API](/metrics-api/)** — Metrics API usage and integration
- **[Monitoring](/monitoring/)** — Monitors, groups, and alerting
- **[Notification](/notification/)** — Notification channels and configuration

## Next Steps

Choose a section from the sidebar or the links above to get started with that part of AlertHawk.
