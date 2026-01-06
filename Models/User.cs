using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BattleOfSea.Models
{
    /// <summary>
    /// Простая модель пользователя (День 11: Авторизация)
    /// </summary>
    public class User : INotifyPropertyChanged
    {
        private string _userId;
        private string _displayName;

        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public User(string userId, string displayName)
        {
            _userId = userId;
            _displayName = displayName;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

