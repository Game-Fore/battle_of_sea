using System.Threading.Tasks;
using Xunit;
using BattleOfSea.ViewModels;
using BattleOfSea.Models;
using System.Threading;
using System;

namespace BattleOfSea.Tests
{
    // Мок-реализация INetworkService для тестирования
    internal class TestNetworkService : BattleOfSea.Services.INetworkService
    {
        public bool IsConnected => true;
        public event Action<BattleOfSea.Models.ShootResultMessage>? ShootResultReceived;
        public event Action<BattleOfSea.Models.ShootMessage>? OpponentShootReceived;
        public event Action<BattleOfSea.Models.GameStateMessage>? GameStateChanged;
        public event Action<BattleOfSea.Models.RoomsListMessage>? RoomsListUpdated;
        public event Action<BattleOfSea.Models.JoinRoomMessage>? JoinRoomResult;
        public event Action<BattleOfSea.Models.UserConnectedMessage>? UserConnected;
        public event Action<string>? ConnectionError;

        public Task<bool> ConnectAsync(string userId, string displayName) => Task.FromResult(true);
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task<System.Collections.Generic.List<Room>> GetRoomsAsync() => Task.FromResult(new System.Collections.Generic.List<Room>());
        public Task<bool> JoinRoomAsync(Room room, string? password = null) => Task.FromResult(true);
        public Task<bool> CreateRoomAsync(Room room, string? password = null) => Task.FromResult(true);
        public Task LeaveRoomAsync() => Task.CompletedTask;
        public Task<bool> SendShootAsync(int row, int col, string roomId) => Task.FromResult(true);
        public Task<bool> SendShipPlacementAsync(System.Collections.Generic.List<ShipPlacementData> ships, string roomId) => Task.FromResult(true);
    }

    // Тесты для модели представления лобби
    public class LobbyViewModelTests
    {
        [Fact]
        // Тест команды присоединения к комнате (публичный метод)
        public async Task JoinCommand_InvokesJoinRequested_OnSuccess()
        {
            var vm = new LobbyViewModel(new TestNetworkService());
            var tcs = new TaskCompletionSource<Room?>();
            vm.JoinRequested += (room) => tcs.TrySetResult(room);

            var room = new Room("JoinMe", 0, 2);
            vm.JoinCommand.Execute(room);

            var timeout = Task.Delay(2000);
            var completed = await Task.WhenAny(tcs.Task, timeout);
            Assert.Equal(tcs.Task, completed);
            Assert.Equal("JoinMe", (await tcs.Task)?.Name);
        }
    }
}