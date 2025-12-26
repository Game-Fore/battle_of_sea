using Avalonia.Controls;

namespace BattleOfSea.Views
{
    public partial class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
            DataContext = new ViewModels.GameViewModel();
        }
    }
}
