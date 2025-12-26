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
            DataContext = new ViewModels.BoardViewModel();
        }

        private void Cell_PointerEnter(object? sender, PointerEventArgs e)
        {
            if (sender is Button b && b.Tag is Models.BoardCell cell)
            {
                b.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
            }
        }

        private void Cell_PointerLeave(object? sender, PointerEventArgs e)
        {
            if (sender is Button b && b.Tag is Models.BoardCell cell)
            {
                b.Background = null;
            }
        }

        private void EnemyCell_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is Models.BoardCell cell)
            {
                VM?.EnemyCellClick(cell);
            }
        }
    }
}
