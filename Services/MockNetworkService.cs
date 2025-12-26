using System;

namespace BattleOfSea.Services
{
    public class MockNetworkService : INetworkService
    {
        public MockNetworkService()
        {
            Console.WriteLine("MockNetworkService created (no network yet)");
        }
    }
}
