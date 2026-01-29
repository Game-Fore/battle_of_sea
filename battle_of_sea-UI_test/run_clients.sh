#!/bin/bash

echo "🎮 Запуск двух клиентов Battle of Sea"
echo ""

# Запуск первого клиента
echo "Запуск КЛИЕНТА 1..."
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj &
CLIENT1_PID=$!

sleep 2

# Запуск второго клиента  
echo ""
echo "Запуск КЛИЕНТА 2..."
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj &
CLIENT2_PID=$!

echo ""
echo "✅ Оба клиента запущены"
echo "Клиент 1 PID: $CLIENT1_PID"
echo "Клиент 2 PID: $CLIENT2_PID"
echo ""
echo "Нажмите Ctrl+C для остановки"

wait
