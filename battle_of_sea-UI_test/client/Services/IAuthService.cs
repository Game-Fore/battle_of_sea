using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    // Сервис авторизации
    public interface IAuthService
    {
        // Текущий пользователь
        User? CurrentUser { get; }
        // Авторизован ли пользователь
        bool IsAuthenticated { get; }
        
        // Вход пользователя
        Task<bool> LoginAsync(string userId, string displayName);
        // Выход пользователя
        Task LogoutAsync();
        // Получение текущего пользователя
        Task<User?> GetCurrentUserAsync();
    }
}