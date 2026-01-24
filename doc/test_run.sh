#!/bin/bash

# Kill all previous processes
pkill -f "battle_of_sea|BattleOfSea" || true
sleep 1

# Clean logs
rm -f /Users/masha/battle_of_sea/logs/*.log

# Start server
echo "Starting server..."
cd /Users/masha/battle_of_sea/battle_of_sea/battle_of_sea
dotnet run > /Users/masha/battle_of_sea/logs/server.log 2>&1 &
SERVER_PID=$!
echo "Server PID: $SERVER_PID"
sleep 2

# Start client 1
echo "Starting client 1..."
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj > /Users/masha/battle_of_sea/logs/client1.log 2>&1 &
CLIENT1_PID=$!
echo "Client 1 PID: $CLIENT1_PID"
sleep 2

# Start client 2
echo "Starting client 2..."
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj > /Users/masha/battle_of_sea/logs/client2.log 2>&1 &
CLIENT2_PID=$!
echo "Client 2 PID: $CLIENT2_PID"
sleep 3

echo ""
echo "Processes started:"
ps aux | grep -E "battle_of_sea|BattleOfSea" | grep -v grep

echo ""
echo "Check logs in /Users/masha/battle_of_sea/logs/"
