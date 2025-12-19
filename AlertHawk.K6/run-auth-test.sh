#!/bin/bash

# Helper script to run K6 authenticated tests with environment variables from .env file
# Usage: ./run-auth-test.sh [test-file] [--dashboard]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K6_BINARY="${K6_BINARY:-k6}"
TEST_FILE="${1:-auth-test.js}"
USE_DASHBOARD="${2:-}"

# Load .env file if it exists
if [ -f "$SCRIPT_DIR/.env" ]; then
    export $(grep -v '^#' "$SCRIPT_DIR/.env" | xargs)
fi

# Check if credentials are set
if [ -z "$K6_AUTH_USERNAME" ] || [ -z "$K6_AUTH_PASSWORD" ]; then
    echo "Error: K6_AUTH_USERNAME and K6_AUTH_PASSWORD must be set"
    echo ""
    echo "You can either:"
    echo "1. Create a .env file with:"
    echo "   K6_AUTH_USERNAME=test@test.com"
    echo "   K6_AUTH_PASSWORD=your_password"
    echo ""
    echo "2. Export environment variables:"
    echo "   export K6_AUTH_USERNAME=test@test.com"
    echo "   export K6_AUTH_PASSWORD=your_password"
    exit 1
fi

if [ ! -f "$SCRIPT_DIR/$TEST_FILE" ]; then
    echo "Error: Test file not found: $TEST_FILE"
    exit 1
fi

# Check if using k6-dashboard
if [ "$USE_DASHBOARD" = "--dashboard" ] || [ -f "$SCRIPT_DIR/k6-dashboard" ]; then
    if [ -f "$SCRIPT_DIR/k6-dashboard" ]; then
        K6_BINARY="$SCRIPT_DIR/k6-dashboard"
        DASHBOARD_FLAG="--out dashboard"
    fi
fi

echo "Starting K6 authenticated test..."
echo "Test file: $TEST_FILE"
echo "Username: $K6_AUTH_USERNAME"
if [ -n "$DASHBOARD_FLAG" ]; then
    echo "Dashboard will be available at: http://localhost:5665"
fi
echo ""
echo "Press Ctrl+C to stop the test"
echo ""

"$K6_BINARY" run $DASHBOARD_FLAG "$SCRIPT_DIR/$TEST_FILE"
