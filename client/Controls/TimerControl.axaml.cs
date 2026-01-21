// Таймер визуал
using Avalonia.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BattleOfSea.Controls
{
    // Таймер управления
    public partial class TimerControl : UserControl, INotifyPropertyChanged
    {
        private string _timeLeftText = "00:30";
        public string TimeLeftText
        {
            get => _timeLeftText;
            set { _timeLeftText = value; OnPropertyChanged(); }
        }

        private int _timeLeftSeconds = 30;
        public int TimeLeftSeconds
        {
            get => _timeLeftSeconds;
            set 
            { 
                _timeLeftSeconds = value;
                TimeLeftText = $"{_timeLeftSeconds / 60:D2}:{_timeLeftSeconds % 60:D2}";
                OnPropertyChanged();
            }
        }

        private Timer? _timer;
        private bool _isRunning = false;

        public TimerControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        // Запустить таймер
        public void StartTimer(int seconds = 30)
        {
            StopTimer();
            TimeLeftSeconds = seconds;
            _isRunning = true;
            _timer = new Timer(TimerCallback, null, 1000, 1000);
        }

        // Остановить таймер
        public void StopTimer()
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }

        // Сброс таймера
        public void ResetTimer(int seconds = 30)
        {
            StopTimer();
            TimeLeftSeconds = seconds;
        }

        // Обратный вызов
        private void TimerCallback(object? state)
        {
            if (!_isRunning) return;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (TimeLeftSeconds > 0)
                {
                    TimeLeftSeconds--;
                }
                else
                {
                    StopTimer();
                    OnTimeExpired?.Invoke();
                }
            });
        }

        // Время истекло
        public event Action? OnTimeExpired;

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            StopTimer();
            base.OnDetachedFromVisualTree(e);
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
