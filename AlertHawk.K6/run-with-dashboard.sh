#!/bin/bash

# Helper script to run K6 tests with xk6-dashboard
# Usage: ./run-with-dashboard.sh [test-file]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K6_DASHBOARD="$SCRIPT_DIR/k6-dashboard"

if [ ! -f "$K6_DASHBOARD" ]; then
    echo "Error: k6-dashboard binary not found at $K6_DASHBOARD"
    exit 1
fi

# Load .env file if it exists
if [ -f "$SCRIPT_DIR/.env" ]; then
    export $(grep -v '^#' "$SCRIPT_DIR/.env" | xargs)
fi

TEST_FILE="${1:-load-test.js}"

if [ ! -f "$SCRIPT_DIR/$TEST_FILE" ]; then
    echo "Error: Test file not found: $TEST_FILE"
    exit 1
fi

echo "Starting K6 test with dashboard..."
echo "Test file: $TEST_FILE"
echo "Dashboard will be available at: http://localhost:5665"
if [ -n "$K6_AUTH_USERNAME" ]; then
    echo "Using authenticated endpoints (username: $K6_AUTH_USERNAME)"
fi
echo ""
echo "Press Ctrl+C to stop the test"
echo ""

"$K6_DASHBOARD" run --out dashboard "$SCRIPT_DIR/$TEST_FILE"
