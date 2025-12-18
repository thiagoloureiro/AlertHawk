# AlertHawk K6 Load Tests

This directory contains K6 performance load tests for the AlertHawk services.

## Prerequisites

- [K6](https://k6.io/docs/getting-started/installation/) installed on your system
- Docker and Docker Compose (for InfluxDB and Grafana setup)
- xk6-dashboard binary (`k6-dashboard` file in this directory)

## Setting Up InfluxDB and Grafana

To visualize and store K6 test results, you can use the provided docker-compose setup:

### Start Services

```bash
docker-compose up -d
```

This will start:
- **InfluxDB** on port `8086` (database: `k6`)
- **Grafana** on port `3000` (default credentials: admin/admin)

### Stop Services

```bash
docker-compose down
```

### Access Grafana

1. Open http://localhost:3000 in your browser
2. Login with username: `admin`, password: `admin`
3. Add InfluxDB as a data source:
   - URL: `http://influxdb:8086`
   - Database: `k6`
   - User: `k6`
   - Password: `k6`

> **Note**: If you have the monitoring service running from the root `docker-compose.yml`, port 8086 may be in use. You may need to stop that service or modify the port mapping in this docker-compose.yml file.

## Running the Tests

### Using xk6-dashboard (Real-time Web Dashboard)

The xk6-dashboard provides a real-time web interface to monitor your tests. To use it:

**Option 1: Using the helper script (recommended)**
```bash
./run-with-dashboard.sh load-test.js
# or for smoke test:
./run-with-dashboard.sh smoke-test.js
```

**Option 2: Direct command**
```bash
./k6-dashboard run --out dashboard load-test.js
```

**Note:** The `--out dashboard` flag is required to enable the dashboard output.

3. Open your browser and navigate to: **http://localhost:5665**

**Optional: Customize dashboard settings**
You can customize the dashboard port and auto-open it in your browser:
```bash
./k6-dashboard run --out 'dashboard=port=5665&open=true' load-test.js
```

Available parameters:
- `port`: TCP port for the dashboard (default: `5665`)
- `open`: Set to `true` to automatically open in browser
- `host`: Hostname or IP address (default: listens on all interfaces)

The dashboard will show real-time metrics including:
- Request rate
- Response times (p50, p95, p99)
- Error rates
- Virtual users
- Data sent/received
- Custom metrics

### Smoke Test (Quick Validation)

Run a quick smoke test to verify all services are responding:

```bash
k6 run smoke-test.js
```

Or with dashboard:
```bash
./run-with-dashboard.sh smoke-test.js
```

Or directly:
```bash
./k6-dashboard run --out dashboard smoke-test.js
```

This test uses minimal load (1 virtual user) to verify basic functionality.

**Note:** If you have credentials set in `.env` or environment variables, the test will also include the authenticated monitoring endpoint.

### Basic Load Test

Run the basic load test that tests all 4 services with gradual ramp-up:

```bash
k6 run load-test.js
```

Or with dashboard:
```bash
./run-with-dashboard.sh load-test.js
```

Or directly:
```bash
./k6-dashboard run --out dashboard load-test.js
```

**Note:** If you have credentials set in `.env` or environment variables, the test will also include the authenticated monitoring endpoint.

### Authenticated Endpoints

All tests (`smoke-test.js`, `load-test.js`) now support authenticated endpoints. If credentials are provided, they will automatically test authenticated endpoints in addition to public endpoints.

**Setup credentials (optional):**

Create a `.env` file in this directory:
```bash
K6_AUTH_USERNAME=test@test.com
K6_AUTH_PASSWORD=your_password_here
```

Or export environment variables:
```bash
export K6_AUTH_USERNAME=test@test.com
export K6_AUTH_PASSWORD=your_password_here
```

**Run tests with authentication:**

The helper scripts automatically load `.env` files:
```bash
# Smoke test with authentication (if credentials are set)
./run-with-dashboard.sh smoke-test.js

# Load test with authentication (if credentials are set)
./run-with-dashboard.sh load-test.js
```

**Dedicated authenticated test:**

The `auth-test.js` file focuses only on authenticated endpoints:

Using the helper script:
```bash
./run-auth-test.sh auth-test.js
```

Or with dashboard:
```bash
./run-auth-test.sh auth-test.js --dashboard
```

Or directly with k6:
```bash
K6_AUTH_USERNAME=test@test.com K6_AUTH_PASSWORD=your_password k6 run auth-test.js
```

**What gets tested:**
- Public endpoints: `/api/version` for all services
- Authenticated endpoint: `/api/MonitorGroup/monitorDashboardGroupListByUser/6` (if credentials are provided)

### With Output Options

You can combine multiple outputs. For example, use both dashboard and InfluxDB:

```bash
./k6-dashboard run --out dashboard --out influxdb=http://localhost:8086/k6 load-test.js
```

This allows you to:
- View real-time results in the dashboard (http://localhost:5665)
- Store historical data in InfluxDB for later analysis in Grafana

**Other output options:**

Run with JSON output:
```bash
k6 run --out json=results.json load-test.js
```

Run with InfluxDB output (requires InfluxDB to be running via docker-compose):
```bash
k6 run --out influxdb=http://localhost:8086/k6 load-test.js
```

Or with authentication (if you set up InfluxDB with auth):
```bash
k6 run --out influxdb=http://k6:k6@localhost:8086/k6 load-test.js
```

Run with Cloud output (K6 Cloud):
```bash
k6 cloud load-test.js
```

## Test Configuration

The current test configuration includes:
- **Ramp-up stages**: Gradually increases load from 0 to 20 virtual users
- **Duration**: Approximately 4 minutes total
- **Thresholds**:
  - 95% of requests should complete in under 500ms
  - Error rate should be under 1%

## Services Tested

### Public Endpoints
1. **Metrics Service**: `https://metrics.alerthawk.net/api/version`
2. **Auth Service**: `https://auth.alerthawk.net/api/version`
3. **Notification Service**: `https://notification.alerthawk.net/api/version`
4. **Monitoring Service**: `https://monitoring.alerthawk.net/api/version`

### Authenticated Endpoints
All authenticated endpoints require: `Authorization: Bearer <token>` header

1. **Auth Login**: `POST https://auth.alerthawk.net/api/auth/login`
   - Payload: `{"username":"test@test.com","password":"..."}`
   - Returns: `{"token":"..."}`

**Monitoring Service:**
2. **Monitoring Dashboard**: `GET https://monitoring.alerthawk.net/api/MonitorGroup/monitorDashboardGroupListByUser/6`
3. **Monitor Alerts**: `GET https://monitoring.alerthawk.net/api/MonitorAlert/monitorAlerts/0/7`
4. **Monitor Group List**: `GET https://monitoring.alerthawk.net/api/MonitorGroup/monitorGroupListByUser`

**Metrics Service:**
5. **Metrics Clusters**: `GET https://metrics.alerthawk.net/api/metrics/clusters`
6. **Metrics Node (with cluster)**: `GET https://metrics.alerthawk.net/api/Metrics/node?minutes=30&clusterName=AKS-01`
7. **Metrics Node**: `GET https://metrics.alerthawk.net/api/Metrics/node?minutes=1`

**Auth Service:**
8. **User Get All**: `GET https://auth.alerthawk.net/api/user/getAll`

**Notification Service:**
9. **Notification List**: `GET https://notification.alerthawk.net/api/Notification/SelectNotificationItemList`

## Customizing Tests

You can modify the `options` object in `load-test.js` to adjust:
- Load stages (number of users, duration)
- Thresholds (response times, error rates)
- Test scenarios

## Test Files

- `smoke-test.js` - Quick validation test for all public endpoints
- `load-test.js` - Basic load test for all public endpoints
- `auth-test.js` - Authenticated test for protected endpoints

## Next Steps

- Add more complex test scenarios
- Add more authenticated endpoint tests
- Add stress tests
- Add spike tests
- Add endurance tests
- Test different user roles and permissions
