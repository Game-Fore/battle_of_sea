// Логика игры
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BattleOfSea.Views
{
    public partial class GameView : UserControl
    {
        private Controls.TimerControl? _timerControl;

        // Конструктор представления игры (публичный)
        public GameView()
        {
            InitializeComponent();
            // DataContext будет установлен хостом (MainWindow), чтобы передать комнату
        }

        // Обработчик изменения контекста данных (защищенный метод переопределения)
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            // Находим таймер после инициализации
            _timerControl = this.FindControl<Controls.TimerControl>("GameTimer");
            
            if (DataContext is ViewModels.GameViewModel gvm && _timerControl != null)
            {
                // Подписываемся на события таймера
                _timerControl.OnTimeExpired += () =>
                {
                    // При истечении времени переключаем ход на противника
                    if (gvm.State == Models.GameState.YourTurn)
                    {
                        gvm.HandleTimeExpired();
                    }
                };
                
                // Подписываемся на изменения состояния игры для управления таймером
                gvm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(ViewModels.GameViewModel.State))
                    {
                        UpdateTimer(gvm);
                    }
                };
                
                // Инициализируем таймер
                UpdateTimer(gvm);
            }
        }

        // Обновление состояния таймера (приватный метод)
        private void UpdateTimer(ViewModels.GameViewModel gvm)
        {
            if (_timerControl == null) return;

            if (gvm.State == Models.GameState.YourTurn && gvm.ShipsPlaced)
            {
                // Запускаем таймер на 30 секунд когда ход игрока
                _timerControl.StartTimer(30);
            }
            else
            {
                // Останавливаем таймер когда ход противника или игра не началась
                _timerControl.StopTimer();
            }
        }

        // Обработчик клика "Выйти в лобби" (приватный метод)
        private async void ExitToLobby_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.GameViewModel gvm)
            {
                // Показываем диалог подтверждения
                var dialog = new ConfirmDialog
                {
                    Message = "Вы уверены, что хотите выйти в лобби? Текущая игра будет завершена."
                };

                var parent = this.VisualRoot as Window;
                if (parent != null)
                {
                    var result = await dialog.ShowDialog<bool?>(parent);
                    if (result == true)
                    {
                        gvm.RequestExit();
                    }
                }
                else
                {
                    // Если не нашли родительское окно, просто выходим
                    gvm.RequestExit();
                }
            }
        }

        // Обработчик клика "Открыть чат" (приватный метод)
        private async void OpenChat_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Создаем окно чата
            var chatWindow = new ChatWindow();
            
            // Получаем родительское окно (MainWindow)
            var parent = this.VisualRoot as Window;
            if (parent != null)
            {
                // Открываем диалоговое окно (блокирует взаимодействие с родителем)
                await chatWindow.ShowDialog(parent);
            }
            else
            {
                // Если не нашли родителя, просто показываем окно
                chatWindow.Show();
            }
        }
    }
}