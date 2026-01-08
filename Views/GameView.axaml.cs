using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BattleOfSea.Views
{
    public partial class GameView : UserControl
    {
        private Controls.TimerControl? _timerControl;

        public GameView()
        {
            InitializeComponent();
            // DataContext will be set by the host (MainWindow) so we can pass a Room
        }

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

        // День 12: UX улучшения - подтверждение выхода
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
    }
}
