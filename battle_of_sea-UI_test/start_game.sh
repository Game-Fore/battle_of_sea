#!/bin/bash

echo "🎮 ЗАПУСК ДВУХ КЛИЕНТОВ BATTLE OF SEA"
echo ""

# Убедимся что сервер запущен
echo "Проверка сервера на порту 5555..."
lsof -i :5555 | grep dotnet > /dev/null && echo "✅ Сервер запущен" || echo "❌ Сервер не запущен!"

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "Запуск КЛИЕНТА 1"
echo "═══════════════════════════════════════════════════════════════"
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj > /tmp/client1.log 2>&1 &
CLIENT1_PID=$!

sleep 3

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "Запуск КЛИЕНТА 2"
echo "═══════════════════════════════════════════════════════════════"
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj > /tmp/client2.log 2>&1 &
CLIENT2_PID=$!

echo ""
echo "✅ Оба клиента запущены!"
echo "Клиент 1 PID: $CLIENT1_PID"
echo "Клиент 2 PID: $CLIENT2_PID"
echo ""
echo "Логи:"
echo "- Сервер: tail -f /tmp/server.log"
echo "- Клиент 1: tail -f /tmp/client1.log"
echo "- Клиент 2: tail -f /tmp/client2.log"
echo ""
echo "Нажмите Ctrl+C для остановки"

wait
