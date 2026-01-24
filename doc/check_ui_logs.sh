#!/bin/bash
# Скрипт для анализа логов после теста UI

echo "============================================================"
echo "АНАЛИЗ ЛОГОВ GAMEVIEWMODEL"
echo "============================================================"

echo ""
echo "📊 1️⃣  ГдеGameStateChanged был получен?"
echo "----"
tail -500 /tmp/server.log 2>/dev/null | grep -A 2 -B 2 "OnGameStateChanged" || echo "❌ Не найдено"

echo ""
echo "📊 2️⃣  State был обновлен в GameViewModel?"
echo "----"
tail -500 /tmp/server.log 2>/dev/null | grep "Current State after assignment" || echo "❌ Не найдено"

echo ""
echo "📊 3️⃣  Ready button был видим?"
echo "----"
tail -500 /tmp/server.log 2>/dev/null | grep "Ready button VISIBLE" || echo "❌ Не найдено"

echo ""
echo "📊 4️⃣  PlayerReady был отправлен?"
echo "----"
tail -500 /tmp/server.log 2>/dev/null | grep -A 3 "PlayerReady" | head -20 || echo "❌ Не найдено"

echo ""
echo "📊 5️⃣  GameStart был отправлен?"
echo "----"
tail -500 /tmp/server.log 2>/dev/null | grep "GameStart sent" || echo "❌ Не найдено"

echo ""
echo "📊 6️⃣  Ошибки в логах?"
echo "----"
tail -500 /tmp/server.log 2>/dev/null | grep -i "error\|exception\|❌" || echo "✅ Ошибок не найдено"

echo ""
echo "============================================================"
echo "ИТОГИ:"
echo "============================================================"
echo ""
echo "Проверьте выше:"
echo "✅ GameStateChanged получено?"
echo "✅ State обновлен в ViewModel?"
echo "✅ Ready button видима?"
echo "✅ PlayerReady отправлен?"
echo "✅ GameStart отправлен?"
echo "✅ Нет ошибок?"
echo ""
echo "Если ВСЕ ✅ - игра работает правильно!"
echo ""
