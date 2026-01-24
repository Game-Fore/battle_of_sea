#!/bin/bash

# Log analyzer script - extracts and displays key DEBUG logs

LOG_DIR="/tmp/battleoftsea_logs"

echo "==========================================================================="
echo "=== LOG ANALYSIS ==="
echo "==========================================================================="
echo ""

if [ ! -d "$LOG_DIR" ]; then
    echo "❌ Log directory not found: $LOG_DIR"
    echo "Run ./run_ui_test.sh first"
    exit 1
fi

echo "📊 SERVER LOGS - PlayerReady Processing"
echo "=========================================="
echo ""
grep -A 15 "HandlePlayerReady START" "$LOG_DIR/server.log" 2>/dev/null | head -50 || echo "No HandlePlayerReady logs found"
echo ""

echo "📊 CLIENT 1 LOGS - GameViewModel Initialization"
echo "=========================================="
echo ""
grep "DEBUG\]\[GameVM\]" "$LOG_DIR/client1.log" 2>/dev/null | head -10 || echo "No GameViewModel logs found"
echo ""

echo "📊 CLIENT 1 LOGS - JoinRoom"
echo "=========================================="
echo ""
grep -A 5 "JoinRoomAsync START" "$LOG_DIR/client1.log" 2>/dev/null | head -20 || echo "No JoinRoom logs found"
echo ""

echo "📊 CLIENT 1 LOGS - SendPlayerReady"
echo "=========================================="
echo ""
grep -A 10 "SendPlayerReadyAsync START" "$LOG_DIR/client1.log" 2>/dev/null | head -30 || echo "No SendPlayerReady logs found"
echo ""

echo "📊 CLIENT 1 LOGS - OnGameStateChanged"
echo "=========================================="
echo ""
grep -A 5 "OnGameStateChanged START" "$LOG_DIR/client1.log" 2>/dev/null | head -30 || echo "No OnGameStateChanged logs found"
echo ""

echo "📊 SUMMARY - Check These Values:"
echo "=========================================="
echo ""
echo "✅ GameViewModel._roomId should be UUID:"
grep "RoomId initialized" "$LOG_DIR/client1.log" 2>/dev/null || echo "❌ Missing GameViewModel initialization log"
echo ""

echo "✅ SendPlayerReadyAsync should have roomId:"
grep "Final roomId for message" "$LOG_DIR/client1.log" 2>/dev/null || echo "❌ Missing roomId in SendPlayerReady"
echo ""

echo "✅ Server should receive both PlayerReady messages:"
echo "Client 1 ready:"
grep "Player1.*is ready" "$LOG_DIR/server.log" 2>/dev/null || echo "❌ Missing Player1 ready"
echo "Client 2 ready:"
grep "Player2.*is ready" "$LOG_DIR/server.log" 2>/dev/null || echo "❌ Missing Player2 ready"
echo ""

echo "✅ Server should send GameStart to both:"
grep "GameStart sent to" "$LOG_DIR/server.log" 2>/dev/null || echo "❌ Missing GameStart messages"
echo ""

echo "✅ Client should receive GameStart:"
grep "GameStart" "$LOG_DIR/client1.log" 2>/dev/null | head -5 || echo "❌ Missing GameStart in client"
echo ""

echo "=========================================="
echo "💡 Full logs available at: $LOG_DIR"
echo "=========================================="
