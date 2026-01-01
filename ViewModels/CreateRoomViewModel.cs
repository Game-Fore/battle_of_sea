using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace BattleOfSea.ViewModels
{
    public class CreateRoomViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _password = string.Empty;
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        private int _maxPlayers = 2;
        public int MaxPlayers { get => _maxPlayers; set { _maxPlayers = value; OnPropertyChanged(); } }

        public ObservableCollection<int> MaxPlayersOptions { get; } = new ObservableCollection<int> { 2, 3, 4 };

        private bool _isPrivate;
        public bool IsPrivate { get => _isPrivate; set { _isPrivate = value; OnPropertyChanged(); } }

        private string? _selectedGameType;
        public string? SelectedGameType { get => _selectedGameType; set { _selectedGameType = value; OnPropertyChanged(); } }

        public ObservableCollection<string> GameTypes { get; } = new ObservableCollection<string> { "Classic", "Timed", "Custom" };

        public CreateRoomViewModel()
        {
        }

        public bool CanCreate => !string.IsNullOrWhiteSpace(Name);
        public string NameError => CanCreate ? string.Empty : "Название комнаты обязательно";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            if (prop == nameof(Name))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCreate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameError)));
            }
        }
    }
}
