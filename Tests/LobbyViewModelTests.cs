using System.Threading.Tasks;
using Xunit;
using BattleOfSea.ViewModels;
using BattleOfSea.Models;
using System.Threading;

namespace BattleOfSea.Tests
{
    public class LobbyViewModelTests
    {
        [Fact]
        public void AddRoom_AddsRoom_WhenNotFull()
        {
            var vm = new LobbyViewModel();
            int initial = vm.Rooms.Count;
            vm.AddRoom(new Room("TestRoom", 0, 2));
            Assert.Equal(initial + 1, vm.Rooms.Count);
            Assert.Equal("TestRoom", vm.Rooms[^1].Name);
        }

        [Fact]
        public async Task JoinCommand_InvokesJoinRequested_OnSuccess()
        {
            var vm = new LobbyViewModel(new Services.MockNetworkService());
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
