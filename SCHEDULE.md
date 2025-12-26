# План на 14 дней (равномерное распределение)

Цель: реализовать основные части игры "Морской бой" (UI, логика, сеть, тесты, CI)

День 1 — Инициализация проекта и статическое лобби
- feat: проект Avalonia + статическое лобби с заглушками
- Commit: `feat(lobby): add static lobby UI with placeholder rooms`

День 2 — Игровые поля, отображение состояний, таймер (текущий день)
- feat(board): add own/enemy boards, hover highlight, click handling on enemy
- feat(state): add game state/status text
- feat(timer): add timer placeholder (circular)
- Commit: `feat(game): add board view, status display and timer placeholder`

День 3 — Модальные окна/создание комнаты
- feat(lobby): add create-room modal (name, password, settings)
- Commit: `feat(lobby): add create-room dialog`

День 4 — Интеграция комнат с навигацией и переход в экран игры
- feat(nav): open game view after joining/creating room
- Commit: `feat(nav): navigate from lobby to game view`

День 5 — Размещение кораблей UI и таймер установки
- feat(board): implement ship placement and placement timer
- Commit: `feat(board): implement ship placement and timer`

День 6 — Основные игровые события (выстрел, ответ, состояние клетки)
- feat(game): implement shooting flow and cell state transitions
- Commit: `feat(game): implement shot handling and results`

День 7 — Логика победы/поражения, подсчёт кораблей
- feat(game): detect sunk ships and win/lose conditions
- Commit: `feat(game): add win/lose detection`

День 8 — Сеть: клиентская архитектура и заглушки сервера
- feat(net): add networking interface and mock server
- Commit: `feat(net): add networking stubs and message definitions`

День 9 — Синхронизация состояний через сеть (протокол)
- feat(net): implement message handling and game synchronization
- Commit: `feat(net): sync game state over network`

День 10 — Лобби онлайн: список комнат из сервера, обновление
- feat(lobby): fetch rooms from server and show statuses
- Commit: `feat(lobby): fetch rooms from server`

День 11 — Авторизация/пользователи (простая) и тесты UI
- feat(auth): add simple user id / display name, add unit tests for UI
- Commit: `feat(auth): add simple user identity`

День 12 — Улучшения UX: подсветка, подтверждения, диалоги
- chore: polish UI interactions and accessibility
- Commit: `chore(ui): improve hover/feedback and dialogs`

День 13 — Тестирование интеграции, багфиксы
- test: add integration tests and fix discovered bugs
- Commit: `test: add integration tests and fix issues`

День 14 — Рефакторинг, документация, CI/CD
- chore: finalize README, CI (github actions), and code cleanup
- Commit: `chore: finalize docs and CI`

---

Каждый день: небольшой, понятный коммит с префиксом (feat, chore, fix, test) и хорошим описанием. Если нужно — могу предложить детальные чек-листы и примеры сообщений для каждого дня.
