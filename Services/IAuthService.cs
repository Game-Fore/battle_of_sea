using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    /// <summary>
    /// Сервис авторизации (День 11: Авторизация/пользователи)
    /// </summary>
    public interface IAuthService
    {
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        
        Task<bool> LoginAsync(string userId, string displayName);
        Task LogoutAsync();
        Task<User?> GetCurrentUserAsync();
    }
}

