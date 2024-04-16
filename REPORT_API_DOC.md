
# API Documentation

## 1. Uptime Report Endpoint

### GET `/monitoring/api/MonitorReport/Uptime/{GroupId}/{Hours}`

This endpoint retrieves a report of uptime and downtime for monitors within a specified group over a given number of hours.

GroupId means a group of application/monitors, for example, Group 1 - AlerHawk, contains 4 monitors, 3 for APIs and 1 for the UI Application.

**Parameters:**

- **GroupId** (path parameter): An integer identifying the group of monitors.
- **Hours** (path parameter): An integer specifying the time span in hours for the report.

**Headers:**

- **ApiKey**: Your API key for authentication.

**Response Format:**

A JSON array of objects, each representing a monitor and its online and offline times in minutes.

**Response Example:**

```json
[
    {
        "monitorName": "AlertHawk Auth API",
        "totalOnlineMinutes": 599,
        "totalOfflineMinutes": 0
    },
    {
        "monitorName": "AlertHawk Monitor API",
        "totalOnlineMinutes": 599,
        "totalOfflineMinutes": 0
    },
    {
        "monitorName": "AlertHawk Notification API",
        "totalOnlineMinutes": 599,
        "totalOfflineMinutes": 0
    },
    {
        "monitorName": "AlertHawk UI (Monitor)",
        "totalOnlineMinutes": 599,
        "totalOfflineMinutes": 0
    }
]
```

**CURL Example:**

```bash
curl -X GET "https://{API_URL}/monitoring/api/MonitorReport/Uptime/1/10" \
     --header "ApiKey: your_api_key"
```

## 2. Alert Report Endpoint

### GET `/monitoring/api/MonitorReport/Alert/{GroupId}/{Hours}`

This endpoint retrieves a report of alerts for monitors within a specified group over a given number of hours.

**Parameters:**

- **GroupId** (path parameter): An integer identifying the group of monitors.
- **Hours** (path parameter): An integer specifying the time span in hours for the report.

**Headers:**

- **ApiKey**: Your API key for authentication.

**Response Format:**

A JSON array of objects, each representing a monitor and the number of alerts it generated within the specified timeframe.

**Response Example:**

```json
[
    {
        "monitorName": "AlertHawk Auth API",
        "numAlerts": 1
    },
    {
        "monitorName": "AlertHawk Monitor API",
        "numAlerts": 1
    }
]
```

**CURL Example:**

```bash
curl -X GET "https://{API_URL}/monitoring/api/MonitorReport/Alert/1/600" \
     --header "ApiKey: your_api_key"
```

## 3. Response Time Report Endpoint

### GET GET /monitoring/api/MonitorReport/ResponseTime/{GroupId}/{Hours}

This endpoint retrieves a report of average, maximum, and minimum response times for monitors within a specified group over a given number of hours.

**Parameters:**

- **GroupId** (path parameter): An integer identifying the group of monitors.
- **Hours** (path parameter): An integer specifying the time span in hours for the report.

**Headers:**

- **ApiKey**: Your API key for authentication.

**Response Format:**

A JSON array of objects, each representing a monitor and its average, maximum, and minimum response times.

**Response Example:**

```json
[
    {
        "monitorName": "AlertHawk Auth API",
        "avgResponseTime": 272,
        "maxResponseTime": 10801,
        "minResponseTime": 0
    },
    {
        "monitorName": "AlertHawk Monitor API",
        "avgResponseTime": 278,
        "maxResponseTime": 9119,
        "minResponseTime": 0
    },
    {
        "monitorName": "AlertHawk Notification API",
        "avgResponseTime": 254,
        "maxResponseTime": 9865,
        "minResponseTime": 0
    },
    {
        "monitorName": "AlertHawk UI (Monitor)",
        "avgResponseTime": 332,
        "maxResponseTime": 15706,
        "minResponseTime": 5
    }
]
```

**CURL Example:**

```bash
curl -X GET "https://{API_URL}/monitoring/api/MonitorReport/ResponseTime/1/600" \
     --header "ApiKey: your_api_key"
```