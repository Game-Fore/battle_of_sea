// Мок аутх
using System;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    /// <summary>
    /// Mock сервис авторизации (День 11)
    /// </summary>
    public class MockAuthService : IAuthService
    {
        private User? _currentUser;

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public async Task<bool> LoginAsync(string userId, string displayName)
        {
            Console.WriteLine($"Mock login: {userId} ({displayName})");
            await Task.Delay(200);
            
            // Простая авторизация - просто создаем пользователя
            _currentUser = new User(userId, displayName);
            return true;
        }

        public async Task LogoutAsync()
        {
            Console.WriteLine("Mock logout");
            await Task.Delay(100);
            _currentUser = null;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            await Task.Delay(50);
            return _currentUser;
        }
    }
}

