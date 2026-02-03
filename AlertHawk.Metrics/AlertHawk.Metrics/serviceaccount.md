# AlertHawk Metrics – Service Account (least privilege)

Create a dedicated namespace, service account, and a **non-admin** ClusterRole with only the permissions required by the collectors.

## 1. Namespace and Service Account

```bash
kubectl create namespace alerthawk

kubectl create serviceaccount alerthawk-sa -n alerthawk
```

## 2. ClusterRole and ClusterRoleBinding

The collectors need:

| Collector            | API / resource              | Verbs / usage |
|----------------------|-----------------------------|----------------|
| EventsCollector      | `events` (core)             | list (namespaced) |
| NodeMetricsCollector | `nodes` (core)             | list, get |
| NodeMetricsCollector | `nodes` (metrics.k8s.io)   | list (cluster custom object) |
| PodMetricsCollector  | `pods` (core)              | list, get (namespaced) |
| PodMetricsCollector  | `pods/log` (core)          | get (when `COLLECT_LOGS=true`) |
| PodMetricsCollector  | `pods` (metrics.k8s.io)    | list (cluster custom object) |
| PvcUsageCollector    | `nodes` (core)             | list, get |
| PvcUsageCollector    | `nodes/proxy` (core)      | get (stats/summary) |
| Version / cloud      | `/version` (non-resource)  | get |

Apply the YAML (ClusterRole + ClusterRoleBinding):

```bash
kubectl apply -f alerthawk-metrics-rbac.yaml
```

See **[alerthawk-metrics-rbac.yaml](alerthawk-metrics-rbac.yaml)** for the full manifest.

## 3. Use this SA in your Deployment/DaemonSet

In the pod spec:

```yaml
spec:
  serviceAccountName: alerthawk-sa
  # ...
```

## Summary

- **Do not** use `cluster-admin` or any admin role.
- The SA `alerthawk-sa` in namespace `alerthawk` has only:
  - read access to **events**, **pods**, **pods/log**, **nodes**, **nodes/proxy**
  - read access to **metrics.k8s.io** **nodes** and **pods**
  - **get** on the non-resource URL **/version**

## Optional: restrict to specific namespaces

If you want to limit list/get of **pods**, **pods/log**, and **events** to certain namespaces only, use a **Role** and **RoleBinding** per namespace instead of cluster-wide access:

```bash
# Example: allow only in namespace "default" and "production"
for ns in default production; do
  kubectl apply -f - <<EOF
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: alerthawk-metrics-collector
  namespace: $ns
rules:
  - apiGroups: [""]
    resources: ["pods", "pods/log", "events"]
    verbs: ["list", "get"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: alerthawk-metrics-collector
  namespace: $ns
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: alerthawk-metrics-collector
subjects:
  - kind: ServiceAccount
    name: alerthawk-sa
    namespace: alerthawk
EOF
done
```

Keep the **ClusterRole** and **ClusterRoleBinding** above for **nodes**, **nodes/proxy**, **metrics.k8s.io**, and **/version**, since those are cluster-scoped or non-namespaced.
