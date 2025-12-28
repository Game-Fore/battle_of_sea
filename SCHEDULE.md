# План на 14 дней (равномерное распределение)

Цель: реализовать основные части игры "Морской бой" (UI, логика, сеть, тесты, CI)

День 1 — Инициализация проекта и статическое лобби
- feat: проект Avalonia + статическое лобби с заглушками

День 2 — Игровые поля, отображение состояний, таймер (текущий день)
- feat(board): add own/enemy boards, hover highlight, click handling on enemy
- feat(state): add game state/status text
- feat(timer): add timer placeholder (circular)

День 3 — Модальные окна/создание комнаты
- feat(lobby): add create-room modal (name, password, settings)

День 4 — Интеграция комнат с навигацией и переход в экран игры
- feat(nav): open game view after joining/creating room


День 5 — Размещение кораблей UI и таймер установки
- feat(board): implement ship placement and placement timer

День 6 — Основные игровые события (выстрел, ответ, состояние клетки)
- feat(game): implement shooting flow and cell state transitions

День 7 — Логика победы/поражения, подсчёт кораблей
- feat(game): detect sunk ships and win/lose conditions

День 8 — Сеть: клиентская архитектура и заглушки сервера
- feat(net): add networking interface and mock server

День 9 — Синхронизация состояний через сеть (протокол)
- feat(net): implement message handling and game synchronization

День 10 — Лобби онлайн: список комнат из сервера, обновление
- feat(lobby): fetch rooms from server and show statuses

День 11 — Авторизация/пользователи (простая) и тесты UI
- feat(auth): add simple user id / display name, add unit tests for UI

День 12 — Улучшения UX: подсветка, подтверждения, диалоги
- chore: polish UI interactions and accessibility

День 13 — Тестирование интеграции, багфиксы
- test: add integration tests and fix discovered bugs

День 14 — Рефакторинг, документация, CI/CD
- chore: finalize README, CI (github actions), and code cleanup
