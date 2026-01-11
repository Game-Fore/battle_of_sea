PROJECT STRUCTURE — полный список файлов (кратко)
Файлы в корне
- `Program.cs` — Старт приложения
- `App.axaml` — Стили приложения
- `App.axaml.cs` — Инициализация приложения
- `MainWindow.axaml` — Главное окно
- `MainWindow.axaml.cs` — Логика окна
- `README.md` — Документация проекта
- `HANDOFF.md` — Интеграция бекенда
- `docs/api_samples.md` — API примеры
- `docs/PROJECT_STRUCTURE.md` — Структура проекта

Папка Views — UI экраны
- `GameView.axaml` — Игровой экран
- `GameView.axaml.cs` — Логика игры
- `LobbyView.axaml` — Экран лобби
- `LobbyView.axaml.cs` — Логика лобби
- `CreateRoomWindow.axaml` — Окно создания
- `CreateRoomWindow.axaml.cs` — Логика создания
- `ShipPlacementView.axaml` — Экран расстановки
- `ShipPlacementView.axaml.cs` — Логика расстановки
- `BoardView.axaml` — Отрисовка поля
- `BoardView.axaml.cs` — Логика поля
- `VictoryDialog.axaml` — Диалог победы
- `VictoryDialog.axaml.cs` — Логика победы
- `ConfirmDialog.axaml` — Диалог подтвержд
- `ConfirmDialog.axaml.cs` — Логика диалога
- `DemoDialog.axaml` — Диалог демо
- `DemoDialog.axaml.cs` — Логика демо

Папка ViewModels — Логика UI
- `GameViewModel.cs` — Управление игрой
- `LobbyViewModel.cs` — Лобби логика
- `CreateRoomViewModel.cs` — Создание комнаты
- `BoardViewModel.cs` — Представление поля
- `ShipPlacementViewModel.cs` — Размещение кораблей

Папка Models — Данные и схемы
- `Board.cs` — Игровое поле
- `BoardCell.cs` — Ячейка поля
- `GameState.cs` — Состояние игры
- `NetworkMessage.cs` — Схемы сообщений
- `ShipPlacement.cs` — Данные кораблей
- `Room.cs` — Комната данных
- `User.cs` — Модель пользователя

Папка Services — Сеть и аутх
- `INetworkService.cs` — Интерфейс сети
- `NetworkService.cs` — Реал WS
- `MockNetworkService.cs` — Мок сети
- `IAuthService.cs` — Интерфейс аутх
- `MockAuthService.cs` — Мок аутх

Папка Controls — Повторно используемые элементы
- `TimerControl.axaml` — Таймер визуал
- `TimerControl.axaml.cs` — Таймер логика

Папка Utils — Вспомогательные классы
- `RelayCommand.cs` — Команда утилита

Дополнительно
- `BattleOfSea.sln` — Файл решения (присутствует)
- `BattleOfSea.csproj` — Файл проекта (присутствует)
