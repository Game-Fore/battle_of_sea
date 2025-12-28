using Avalonia.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BattleOfSea.Controls
{
    public partial class TimerControl : UserControl, INotifyPropertyChanged
    {
        private string _timeLeftText = "00:30";
        public string TimeLeftText
        {
            get => _timeLeftText;
            set { _timeLeftText = value; OnPropertyChanged(); }
        }

        public TimerControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
