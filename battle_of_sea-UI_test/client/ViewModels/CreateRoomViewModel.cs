// Логика создания
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace BattleOfSea.ViewModels
{
    // Модель представления создания комнаты
    public class CreateRoomViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        // Название комнаты (публичное свойство)
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _password = string.Empty;
        // Пароль комнаты (публичное свойство)
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        private int _maxPlayers = 2;
        // Максимальное количество игроков (публичное свойство)
        public int MaxPlayers { get => _maxPlayers; set { _maxPlayers = value; OnPropertyChanged(); } }

        // Опции количества игроков (публичное свойство)
        public ObservableCollection<int> MaxPlayersOptions { get; } = new ObservableCollection<int> { 2, 3, 4 };

        private bool _isPrivate;
        // Приватность комнаты (публичное свойство)
        public bool IsPrivate { get => _isPrivate; set { _isPrivate = value; OnPropertyChanged(); } }

        private string? _selectedGameType;
        // Выбранный тип игры (публичное свойство)
        public string? SelectedGameType { get => _selectedGameType; set { _selectedGameType = value; OnPropertyChanged(); } }

        // Доступные типы игр (публичное свойство)
        public ObservableCollection<string> GameTypes { get; } = new ObservableCollection<string> { "Classic", "Timed", "Custom" };

        // Конструктор модели представления (публичный)
        public CreateRoomViewModel()
        {
        }

        // Можно ли создать комнату (публичное свойство)
        public bool CanCreate => !string.IsNullOrWhiteSpace(Name);
        // Ошибка в названии комнаты (публичное свойство)
        public string NameError => CanCreate ? string.Empty : "Название комнаты обязательно";

        // Событие изменения свойства (публичное событие)
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства (приватный метод)
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