using Xunit;
using BattleOfSea.ViewModels;

namespace BattleOfSea.Tests
{
    // Тесты для модели представления создания комнаты
    public class CreateRoomViewModelTests
    {
        [Fact]
        // Тест проверки возможности создания комнаты при пустом имени (публичный метод)
        public void CanCreate_IsFalse_WhenNameEmpty()
        {
            var vm = new CreateRoomViewModel();
            Assert.False(vm.CanCreate);
            Assert.True(!string.IsNullOrEmpty(vm.NameError) && vm.CanCreate == false);
        }

        [Fact]
        // Тест проверки возможности создания комнаты при указанном имени (публичный метод)
        public void CanCreate_IsTrue_WhenNameProvided()
        {
            var vm = new CreateRoomViewModel();
            vm.Name = "Room 1";
            Assert.True(vm.CanCreate);
            Assert.Equal(string.Empty, vm.NameError);
        }
    }
}