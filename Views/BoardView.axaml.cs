using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace BattleOfSea.Views
{
    public partial class BoardView : UserControl
    {
        private ViewModels.BoardViewModel? VM => DataContext as ViewModels.BoardViewModel;

        public BoardView()
        {
            InitializeComponent();
            // Do not set DataContext here; expect parent (GameView) to provide GameViewModel
        }


        private void EnemyCell_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is Models.BoardCell cell)
            {
                // If the view is hosted inside GameView, call its Shoot handler
                if (DataContext is ViewModels.GameViewModel gvm)
                {
                    gvm.ShootAt(cell);
                }
                else
                {
                    VM?.EnemyCellClick(cell);
                }
            }
        }
    }
}
