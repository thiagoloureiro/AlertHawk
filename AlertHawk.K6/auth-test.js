import http from "k6/http";
import { check, sleep } from "k6";
import { Rate } from "k6/metrics";

// Custom metrics
const errorRate = new Rate("errors");

// Test configuration
export const options = {
  stages: [
    { duration: "30s", target: 5 }, // Ramp up to 5 users over 30 seconds
    { duration: "1m", target: 5 }, // Stay at 5 users for 1 minute
    { duration: "30s", target: 10 }, // Ramp up to 10 users over 30 seconds
    { duration: "1m", target: 10 }, // Stay at 10 users for 1 minute
    { duration: "30s", target: 0 }, // Ramp down to 0 users over 30 seconds
  ],
  thresholds: {
    http_req_duration: ["p(95)<1000"], // 95% of requests should be below 1s
    http_req_failed: ["rate<0.01"], // Error rate should be less than 1%
    errors: ["rate<0.01"], // Custom error rate should be less than 1%
  },
};

// Get credentials from environment variables
const authUsername = __ENV.K6_AUTH_USERNAME || "test@test.com";
const authPassword = __ENV.K6_AUTH_PASSWORD || "";

// Base URLs
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

// Setup function runs once per VU before the main function
export function setup() {
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
    console.error("Response:", loginResponse.body);
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
  // Check if we have a valid token
  if (!data.token) {
    console.error("No token available, skipping authenticated requests");
    errorRate.add(1);
    return;
  }

  const params = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
    },
  };

  // Test monitoring dashboard
  let response = http.get(authenticatedEndpoints.monitoringDashboard, params);
  let checkResult = check(response, {
    "monitoring dashboard status is 200": (r) => r.status === 200,
    "monitoring dashboard response time < 1000ms": (r) => r.timings.duration < 1000,
    "monitoring dashboard has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);

  // Test monitor alerts
  response = http.get(authenticatedEndpoints.monitorAlerts, params);
  checkResult = check(response, {
    "monitor alerts status is 200": (r) => r.status === 200,
    "monitor alerts response time < 1000ms": (r) => r.timings.duration < 1000,
    "monitor alerts has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);

  // Test monitor group list
  response = http.get(authenticatedEndpoints.monitorGroupList, params);
  checkResult = check(response, {
    "monitor group list status is 200": (r) => r.status === 200,
    "monitor group list response time < 1000ms": (r) => r.timings.duration < 1000,
    "monitor group list has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);

  // Test metrics clusters
  response = http.get(authenticatedEndpoints.metricsClusters, params);
  checkResult = check(response, {
    "metrics clusters status is 200": (r) => r.status === 200,
    "metrics clusters response time < 1000ms": (r) => r.timings.duration < 1000,
    "metrics clusters has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);

  // Test metrics node with cluster
  response = http.get(authenticatedEndpoints.metricsNodeWithCluster, params);
  checkResult = check(response, {
    "metrics node with cluster status is 200": (r) => r.status === 200,
    "metrics node with cluster response time < 1000ms": (r) => r.timings.duration < 1000,
    "metrics node with cluster has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);

  // Test metrics node
  response = http.get(authenticatedEndpoints.metricsNode, params);
  checkResult = check(response, {
    "metrics node status is 200": (r) => r.status === 200,
    "metrics node response time < 1000ms": (r) => r.timings.duration < 1000,
    "metrics node has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);

  // Test user get all
  response = http.get(authenticatedEndpoints.userGetAll, params);
  checkResult = check(response, {
    "user get all status is 200": (r) => r.status === 200,
    "user get all response time < 1000ms": (r) => r.timings.duration < 1000,
    "user get all has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);

  // Test notification list
  response = http.get(authenticatedEndpoints.notificationList, params);
  checkResult = check(response, {
    "notification list status is 200": (r) => r.status === 200,
    "notification list response time < 1000ms": (r) => r.timings.duration < 1000,
    "notification list has response body": (r) => r.body.length > 0,
  });
  errorRate.add(!checkResult);
  sleep(1);
}
