using System;
using System.Threading.Tasks;

namespace BattleOfSea.Services
{
    public class MockNetworkService : INetworkService
    {
        public MockNetworkService()
        {
            Console.WriteLine("MockNetworkService created (no network yet)");
        }

        public async Task<bool> JoinRoomAsync(Models.Room room)
        {
            Console.WriteLine($"Mock join request for room: {room.Name}");
            await Task.Delay(300); // simulate network latency
            return true;
        }

        public async Task<bool> CreateRoomAsync(Models.Room room)
        {
            Console.WriteLine($"Mock create room request: {room.Name}");
            await Task.Delay(300);
            return true;
        }
    }
}
