using System.Threading.Tasks;

namespace BattleOfSea.Services
{
    public interface INetworkService
    {
        Task<bool> JoinRoomAsync(Models.Room room);
        Task<bool> CreateRoomAsync(Models.Room room);
    }
}
