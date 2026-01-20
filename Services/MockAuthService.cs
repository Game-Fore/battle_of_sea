// Мок аутх
using System;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    // Заглушка сервиса авторизации для тестирования
    public class MockAuthService : IAuthService
    {
        private User? _currentUser;

        // Текущий пользователь (публичное свойство)
        public User? CurrentUser => _currentUser;
        // Авторизован ли пользователь (публичное свойство)
        public bool IsAuthenticated => _currentUser != null;

        // Вход пользователя (публичный метод)
        public async Task<bool> LoginAsync(string userId, string displayName)
        {
            Console.WriteLine($"Mock login: {userId} ({displayName})");
            await Task.Delay(200);
            
            // Создание пользователя для тестирования
            _currentUser = new User(userId, displayName);
            return true;
        }

        // Выход пользователя (публичный метод)
        public async Task LogoutAsync()
        {
            Console.WriteLine("Mock logout");
            await Task.Delay(100);
            _currentUser = null;
        }

        // Получение текущего пользователя (публичный метод)
        public async Task<User?> GetCurrentUserAsync()
        {
            await Task.Delay(50);
            return _currentUser;
        }
    }
}