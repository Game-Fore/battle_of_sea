#!/bin/bash

# Test script: Start server and two Avalonia clients for testing
# This script captures logs from all processes for debugging

set -e

echo "==========================================================================="
echo "=== BATTLE OF SEA - UI CLIENT TEST WITH LOGGING ==="
echo "==========================================================================="

PROJECT_DIR="/Users/masha/battle_of_sea"
SERVER_DIR="$PROJECT_DIR/battle_of_sea/battle_of_sea"
CLIENT_DIR="$PROJECT_DIR/client"

LOG_DIR="/tmp/battleoftsea_logs"
mkdir -p "$LOG_DIR"

SERVER_LOG="$LOG_DIR/server.log"
CLIENT1_LOG="$LOG_DIR/client1.log"
CLIENT2_LOG="$LOG_DIR/client2.log"

# Clean up previous logs
rm -f "$SERVER_LOG" "$CLIENT1_LOG" "$CLIENT2_LOG"

echo ""
echo "📝 Logs will be saved to: $LOG_DIR"
echo ""

# Function to kill all processes on script exit
cleanup() {
    echo ""
    echo "🛑 Cleaning up..."
    pkill -P $$ || true
    sleep 1
}
trap cleanup EXIT

echo "=========================================="
echo "🚀 Step 1: Starting WebSocket Server"
echo "=========================================="
cd "$SERVER_DIR"
echo "[$(date '+%H:%M:%S')] Starting server..."
nohup dotnet run --project battle_of_sea.csproj > "$SERVER_LOG" 2>&1 &
SERVER_PID=$!
echo "✅ Server PID: $SERVER_PID"

# Wait for server to start
sleep 3

# Check if server is running
if ! ps -p $SERVER_PID > /dev/null; then
    echo "❌ Server failed to start!"
    cat "$SERVER_LOG"
    exit 1
fi

# Check if port is open
if ! lsof -i :5555 > /dev/null 2>&1; then
    echo "❌ Server not listening on port 5555!"
    sleep 2
    lsof -i :5555 || echo "Port 5555 not in use"
    exit 1
fi

echo "✅ Server is running on port 5555"
echo ""

echo "=========================================="
echo "🎮 Step 2: Starting Client 1 (Room Creator)"
echo "=========================================="
cd "$CLIENT_DIR"
echo "[$(date '+%H:%M:%S')] Starting client 1..."
dotnet run --project BattleOfSea.csproj > "$CLIENT1_LOG" 2>&1 &
CLIENT1_PID=$!
echo "✅ Client 1 PID: $CLIENT1_PID"
sleep 2
echo ""

echo "=========================================="
echo "🎮 Step 3: Starting Client 2 (Room Joiner)"
echo "=========================================="
echo "[$(date '+%H:%M:%S')] Starting client 2..."
dotnet run --project BattleOfSea.csproj > "$CLIENT2_LOG" 2>&1 &
CLIENT2_PID=$!
echo "✅ Client 2 PID: $CLIENT2_PID"
sleep 2
echo ""

echo "=========================================="
echo "📋 MANUAL TEST STEPS:"
echo "=========================================="
echo ""
echo "🔵 CLIENT 1 (already running):"
echo "  1. Enter username: Player1"
echo "  2. Click Connect"
echo "  3. Click Create Room"
echo "  4. Enter room name: TestRoom"
echo "  5. Wait for Client 2 to join"
echo ""
echo "🔵 CLIENT 2 (already running):"
echo "  1. Enter username: Player2"
echo "  2. Click Connect"
echo "  3. Select room from list"
echo "  4. Click Join Room"
echo ""
echo "🔵 BOTH CLIENTS:"
echo "  5. When room shows 'Ready to Start', click Ready button"
echo "  6. Game should transition to YourTurn/OpponentTurn"
echo ""
echo "=========================================="
echo "📊 MONITORING:"
echo "=========================================="
echo ""
echo "📌 To view logs in real-time:"
echo "   - Server:  tail -f $SERVER_LOG"
echo "   - Client1: tail -f $CLIENT1_LOG"
echo "   - Client2: tail -f $CLIENT2_LOG"
echo ""
echo "📌 Search for specific logs:"
echo "   - grep -i 'DEBUG' $SERVER_LOG"
echo "   - grep -i 'DEBUG' $CLIENT1_LOG"
echo "   - grep -i 'PlayerReady' $SERVER_LOG"
echo "   - grep -i 'GameStart' $CLIENT1_LOG"
echo ""
echo "=========================================="
echo "⏸️  Press Ctrl+C to stop all processes"
echo "=========================================="
echo ""

# Wait for all processes
wait $SERVER_PID $CLIENT1_PID $CLIENT2_PID 2>/dev/null || true

echo "✅ Test completed. Logs saved to $LOG_DIR"
