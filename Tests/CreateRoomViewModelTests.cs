using Xunit;
using BattleOfSea.ViewModels;

namespace BattleOfSea.Tests
{
    public class CreateRoomViewModelTests
    {
        [Fact]
        public void CanCreate_IsFalse_WhenNameEmpty()
        {
            var vm = new CreateRoomViewModel();
            Assert.False(vm.CanCreate);
            Assert.True(!string.IsNullOrEmpty(vm.NameError) && vm.CanCreate == false);
        }

        [Fact]
        public void CanCreate_IsTrue_WhenNameProvided()
        {
            var vm = new CreateRoomViewModel();
            vm.Name = "Room 1";
            Assert.True(vm.CanCreate);
            Assert.Equal(string.Empty, vm.NameError);
        }
    }
}
