#!/bin/bash

# Real-time log monitor - shows critical events as they happen

LOG_DIR="/tmp/battleoftsea_logs"

echo "==========================================================================="
echo "=== REAL-TIME LOG MONITOR ==="
echo "==========================================================================="
echo ""
echo "📌 Make sure to run ./run_ui_test.sh in another terminal first"
echo ""
echo "Following events:"
echo "  • GameViewModel initialization"
echo "  • Room join"
echo "  • PlayerReady sent/received"
echo "  • GameStart sent/received"
echo ""
echo "Press Ctrl+C to stop monitoring"
echo ""

# Create combined log file
COMBINED_LOG="/tmp/combined.log"

monitor_event() {
    local pattern=$1
    local label=$2
    local file=$3
    local color=$4
    
    if [ -f "$file" ]; then
        tail -f "$file" | grep --line-buffered "$pattern" | while read line; do
            echo -e "${color}[$label] $line\033[0m"
        done &
    fi
}

# Start monitoring all files
echo "🔵 Monitoring Server..."
monitor_event "DEBUG\|PlayerReady\|GameStart" "SERVER" "$LOG_DIR/server.log" "\033[34m" &

echo "🟢 Monitoring Client 1..."
monitor_event "DEBUG" "CLIENT1" "$LOG_DIR/client1.log" "\033[32m" &

echo "🟠 Monitoring Client 2..."
monitor_event "DEBUG" "CLIENT2" "$LOG_DIR/client2.log" "\033[33m" &

# Wait for all background processes
wait
