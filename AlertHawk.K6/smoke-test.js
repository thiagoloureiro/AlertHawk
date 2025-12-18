import http from 'k6/http';
import { check } from 'k6';

// Smoke test - minimal load to verify the system works
export const options = {
  vus: 1,  // 1 virtual user
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests should be below 1s
    http_req_failed: ['rate<0.01'],   // Error rate should be less than 1%
  },
};

// Get credentials from environment variables (optional)
const authUsername = __ENV.K6_AUTH_USERNAME || '';
const authPassword = __ENV.K6_AUTH_PASSWORD || '';
const hasCredentials = authUsername !== '' && authPassword !== '';

// Base URLs for the services
const services = {
  metrics: 'https://metrics.alerthawk.net/api/version',
  auth: 'https://auth.alerthawk.net/api/version',
  notification: 'https://notification.alerthawk.net/api/version',
  monitoring: 'https://monitoring.alerthawk.net/api/version',
};

// Authenticated endpoints
const authUrl = 'https://auth.alerthawk.net/api/auth/login';
const authenticatedEndpoints = {
  monitoringDashboard: 'https://monitoring.alerthawk.net/api/MonitorGroup/monitorDashboardGroupListByUser/6',
  monitorAlerts: 'https://monitoring.alerthawk.net/api/MonitorAlert/monitorAlerts/0/7',
  monitorGroupList: 'https://monitoring.alerthawk.net/api/MonitorGroup/monitorGroupListByUser',
  metricsClusters: 'https://metrics.alerthawk.net/api/metrics/clusters',
  metricsNodeWithCluster: 'https://metrics.alerthawk.net/api/Metrics/node?minutes=30&clusterName=AKS-01',
  metricsNode: 'https://metrics.alerthawk.net/api/Metrics/node?minutes=1',
  userGetAll: 'https://auth.alerthawk.net/api/user/getAll',
  notificationList: 'https://notification.alerthawk.net/api/Notification/SelectNotificationItemList',
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
      'Content-Type': 'application/json',
    },
  };

  const loginResponse = http.post(authUrl, loginPayload, loginParams);

  const loginCheck = check(loginResponse, {
    'login status is 200': (r) => r.status === 200,
    'login has token': (r) => {
      if (r.status === 200) {
        try {
          const body = JSON.parse(r.body);
          return body.token !== undefined && body.token !== null && body.token !== '';
        } catch (e) {
          return false;
        }
      }
      return false;
    },
  });

  if (!loginCheck) {
    console.error('Failed to authenticate. Status:', loginResponse.status);
    return { token: null };
  }

  try {
    const body = JSON.parse(loginResponse.body);
    if (body.token && body.token !== '') {
      return { token: body.token };
    } else {
      console.error('Token is empty in login response');
      return { token: null };
    }
  } catch (e) {
    console.error('Failed to parse login response:', e);
    return { token: null };
  }
}

export default function (data) {
  // Test Metrics service
  let response = http.get(services.metrics);
  check(response, {
    'metrics status is 200': (r) => r.status === 200,
    'metrics has response body': (r) => r.body.length > 0,
  });

  // Test Auth service
  response = http.get(services.auth);
  check(response, {
    'auth status is 200': (r) => r.status === 200,
    'auth has response body': (r) => r.body.length > 0,
  });

  // Test Notification service
  response = http.get(services.notification);
  check(response, {
    'notification status is 200': (r) => r.status === 200,
    'notification has response body': (r) => r.body.length > 0,
  });

  // Test Monitoring service
  response = http.get(services.monitoring);
  check(response, {
    'monitoring status is 200': (r) => r.status === 200,
    'monitoring has response body': (r) => r.body.length > 0,
  });

  // Test authenticated endpoints (if credentials are provided)
  if (data && data.token) {
    const authParams = {
      headers: {
        'Authorization': `Bearer ${data.token}`,
        'Content-Type': 'application/json',
      },
    };

    // Test monitoring dashboard
    response = http.get(authenticatedEndpoints.monitoringDashboard, authParams);
    check(response, {
      'monitoring dashboard status is 200': (r) => r.status === 200,
      'monitoring dashboard has response body': (r) => r.body.length > 0,
    });

    // Test monitor alerts
    response = http.get(authenticatedEndpoints.monitorAlerts, authParams);
    check(response, {
      'monitor alerts status is 200': (r) => r.status === 200,
      'monitor alerts has response body': (r) => r.body.length > 0,
    });

    // Test monitor group list
    response = http.get(authenticatedEndpoints.monitorGroupList, authParams);
    check(response, {
      'monitor group list status is 200': (r) => r.status === 200,
      'monitor group list has response body': (r) => r.body.length > 0,
    });

    // Test metrics clusters
    response = http.get(authenticatedEndpoints.metricsClusters, authParams);
    check(response, {
      'metrics clusters status is 200': (r) => r.status === 200,
      'metrics clusters has response body': (r) => r.body.length > 0,
    });

    // Test metrics node with cluster
    response = http.get(authenticatedEndpoints.metricsNodeWithCluster, authParams);
    check(response, {
      'metrics node with cluster status is 200': (r) => r.status === 200,
      'metrics node with cluster has response body': (r) => r.body.length > 0,
    });

    // Test metrics node
    response = http.get(authenticatedEndpoints.metricsNode, authParams);
    check(response, {
      'metrics node status is 200': (r) => r.status === 200,
      'metrics node has response body': (r) => r.body.length > 0,
    });

    // Test user get all
    response = http.get(authenticatedEndpoints.userGetAll, authParams);
    check(response, {
      'user get all status is 200': (r) => r.status === 200,
      'user get all has response body': (r) => r.body.length > 0,
    });

    // Test notification list
    response = http.get(authenticatedEndpoints.notificationList, authParams);
    check(response, {
      'notification list status is 200': (r) => r.status === 200,
      'notification list has response body': (r) => r.body.length > 0,
    });
  }
}
