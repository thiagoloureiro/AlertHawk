import http from "k6/http";
import { check, sleep } from "k6";
import { Rate } from "k6/metrics";

// Custom metrics
const errorRate = new Rate("errors");

// Test configuration
export const options = {
  stages: [
    { duration: "2m", target: 25 }, // Ramp up to 25 users over 2 minutes
    { duration: "2m", target: 50 }, // Ramp up to 50 users over 2 minutes
    { duration: "2m", target: 75 }, // Ramp up to 75 users over 2 minutes
    { duration: "2m", target: 100 }, // Ramp up to 100 users over 2 minutes
    { duration: "1m", target: 100 }, // Stay at 100 users for 1 minute
    { duration: "1m", target: 0 }, // Ramp down to 0 users over 1 minute
  ],
  thresholds: {
    http_req_duration: ["p(95)<3000", "p(99)<5000"], // 95% below 3s, 99% below 5s (adjusted for 100 users)
    http_req_failed: ["rate<0.05"], // Error rate should be less than 5% (more lenient for high load)
    errors: ["rate<0.05"], // Custom error rate should be less than 5%
  },
};

// Get credentials from environment variables (optional)
const authUsername = __ENV.K6_AUTH_USERNAME || "";
const authPassword = __ENV.K6_AUTH_PASSWORD || "";
const hasCredentials = authUsername !== "" && authPassword !== "";

// Base URLs for the services
const services = {
  metrics: "https://metrics.alerthawk.net/api/version",
  auth: "https://auth.alerthawk.net/api/version",
  notification: "https://notification.alerthawk.net/api/version",
  monitoring: "https://monitoring.alerthawk.net/api/version",
};

// Authenticated endpoints
const authUrl = "https://auth.alerthawk.net/api/auth/login";
const authenticatedEndpoints = {
  monitoringDashboard: "https://monitoring.alerthawk.net/api/MonitorGroup/monitorDashboardGroupListByUser/6",
  monitorAlerts: "https://monitoring.alerthawk.net/api/MonitorAlert/monitorAlerts/0/7",
  monitorGroupList: "https://monitoring.alerthawk.net/api/MonitorGroup/monitorGroupListByUser",
  metricsClusters: "https://metrics.alerthawk.net/api/metrics/clusters",
  metricsNodeWithCluster: "https://metrics.alerthawk.net/api/Metrics/node?minutes=30&clusterName=AKS-01",
  metricsNode: "https://metrics.alerthawk.net/api/Metrics/node?minutes=1",
  userGetAll: "https://auth.alerthawk.net/api/user/getAll",
  notificationList: "https://notification.alerthawk.net/api/Notification/SelectNotificationItemList",
};

// Setup function runs once per VU before the main function (only if credentials are provided)
export function setup() {
  if (!hasCredentials) {
    return { token: null };
  }

  // Authenticate and get token
  const loginPayload = JSON.stringify({
    username: authUsername,
    password: authPassword,
  });

  const loginParams = {
    headers: {
      "Content-Type": "application/json",
    },
  };

  const loginResponse = http.post(authUrl, loginPayload, loginParams);

  const loginCheck = check(loginResponse, {
    "login status is 200": (r) => r.status === 200,
    "login has token": (r) => {
      if (r.status === 200) {
        try {
          const body = JSON.parse(r.body);
          return body.token !== undefined && body.token !== null && body.token !== "";
        } catch (e) {
          return false;
        }
      }
      return false;
    },
  });

  if (!loginCheck) {
    console.error("Failed to authenticate. Status:", loginResponse.status);
    return { token: null };
  }

  try {
    const body = JSON.parse(loginResponse.body);
    if (body.token && body.token !== "") {
      return { token: body.token };
    } else {
      console.error("Token is empty in login response");
      return { token: null };
    }
  } catch (e) {
    console.error("Failed to parse login response:", e);
    return { token: null };
  }
}

export default function (data) {
  // Test Metrics service
  let response = http.get(services.metrics);
  const metricsCheck = check(response, {
    "metrics status is 200": (r) => r.status === 200,
    "metrics response time < 500ms": (r) => r.timings.duration < 500,
  });
  errorRate.add(!metricsCheck);

  sleep(1);

  // Test Auth service
  response = http.get(services.auth);
  const authCheck = check(response, {
    "auth status is 200": (r) => r.status === 200,
    "auth response time < 500ms": (r) => r.timings.duration < 500,
  });
  errorRate.add(!authCheck);

  sleep(1);

  // Test Notification service
  response = http.get(services.notification);
  const notificationCheck = check(response, {
    "notification status is 200": (r) => r.status === 200,
    "notification response time < 500ms": (r) => r.timings.duration < 500,
  });
  errorRate.add(!notificationCheck);

  sleep(1);

  // Test Monitoring service
  response = http.get(services.monitoring);
  const monitoringCheck = check(response, {
    "monitoring status is 200": (r) => r.status === 200,
    "monitoring response time < 500ms": (r) => r.timings.duration < 500,
  });
  errorRate.add(!monitoringCheck);

  sleep(1);

  // Test authenticated endpoints (if credentials are provided)
  if (data && data.token) {
    const authParams = {
      headers: {
        Authorization: `Bearer ${data.token}`,
        "Content-Type": "application/json",
      },
    };

    // Test monitoring dashboard
    response = http.get(authenticatedEndpoints.monitoringDashboard, authParams);
    const monitoringDashboardCheck = check(response, {
      "monitoring dashboard status is 200": (r) => r.status === 200,
      "monitoring dashboard response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!monitoringDashboardCheck);
    sleep(1);

    // Test monitor alerts
    response = http.get(authenticatedEndpoints.monitorAlerts, authParams);
    const monitorAlertsCheck = check(response, {
      "monitor alerts status is 200": (r) => r.status === 200,
      "monitor alerts response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!monitorAlertsCheck);
    sleep(1);

    // Test monitor group list
    response = http.get(authenticatedEndpoints.monitorGroupList, authParams);
    const monitorGroupListCheck = check(response, {
      "monitor group list status is 200": (r) => r.status === 200,
      "monitor group list response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!monitorGroupListCheck);
    sleep(1);

    // Test metrics clusters
    response = http.get(authenticatedEndpoints.metricsClusters, authParams);
    const metricsClustersCheck = check(response, {
      "metrics clusters status is 200": (r) => r.status === 200,
      "metrics clusters response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!metricsClustersCheck);
    sleep(1);

    // Test metrics node with cluster
    response = http.get(authenticatedEndpoints.metricsNodeWithCluster, authParams);
    const metricsNodeWithClusterCheck = check(response, {
      "metrics node with cluster status is 200": (r) => r.status === 200,
      "metrics node with cluster response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!metricsNodeWithClusterCheck);
    sleep(1);

    // Test metrics node
    response = http.get(authenticatedEndpoints.metricsNode, authParams);
    const metricsNodeCheck = check(response, {
      "metrics node status is 200": (r) => r.status === 200,
      "metrics node response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!metricsNodeCheck);
    sleep(1);

    // Test user get all
    response = http.get(authenticatedEndpoints.userGetAll, authParams);
    const userGetAllCheck = check(response, {
      "user get all status is 200": (r) => r.status === 200,
      "user get all response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!userGetAllCheck);
    sleep(1);

    // Test notification list
    response = http.get(authenticatedEndpoints.notificationList, authParams);
    const notificationListCheck = check(response, {
      "notification list status is 200": (r) => r.status === 200,
      "notification list response time < 500ms": (r) => r.timings.duration < 500,
    });
    errorRate.add(!notificationListCheck);
    sleep(1);
  }
}
