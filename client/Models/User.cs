// Пользователь модели
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BattleOfSea.Models
{
    // Модель пользователя
    public class User : INotifyPropertyChanged
    {
        private string _userId;
        private string _displayName;

        // ID пользователя
        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        // Отображаемое имя
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        // Конструктор пользователя
        public User(string userId, string displayName)
        {
            _userId = userId;
            _displayName = displayName;
        }

        // Событие изменения свойства
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}