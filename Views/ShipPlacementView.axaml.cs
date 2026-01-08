using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Linq;

namespace BattleOfSea.Views
{
    public partial class ShipPlacementView : UserControl
    {
        private const int CellSize = 30;
        private const int HeaderSize = 28;
        private System.Collections.Generic.Dictionary<Models.BoardCell, Control> _cellControls = new();

        public ShipPlacementView()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (IsInitialized)
            {
                BuildPlacementBoard();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            BuildPlacementBoard();
        }

        private void BuildPlacementBoard()
        {
            if (DataContext is ViewModels.GameViewModel gvm)
            {
                BuildBoard(PlacementBoardGrid, gvm.Own, gvm);
            }
        }

        private void BuildBoard(Grid grid, Models.Board board, ViewModels.GameViewModel gvm)
        {
            grid.Children.Clear();
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            int size = board.Size;
            
            // Устанавливаем фиксированный размер для квадратного поля 10x10
            int totalSize = HeaderSize + size * CellSize; // 28 + 10*30 = 328 пикселей
            grid.Width = totalSize;
            grid.Height = totalSize;
            grid.MinWidth = totalSize;
            grid.MaxWidth = totalSize;
            grid.MinHeight = totalSize;
            grid.MaxHeight = totalSize;

            // Create column definitions: header + size columns (all same size)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(HeaderSize, GridUnitType.Pixel) });
            for (int i = 0; i < size; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSize, GridUnitType.Pixel), MinWidth = CellSize, MaxWidth = CellSize });
            }

            // Create row definitions: header + size rows (all same size)
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(HeaderSize, GridUnitType.Pixel) });
            for (int i = 0; i < size; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSize, GridUnitType.Pixel), MinHeight = CellSize, MaxHeight = CellSize });
            }

            // Add column headers (A-J)
            for (int col = 0; col < size; col++)
            {
                var headerBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    ZIndex = 10
                };
                var header = new TextBlock
                {
                    Text = ((char)('A' + col)).ToString(),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0))
                };
                headerBorder.Child = header;
                Grid.SetColumn(headerBorder, col + 1);
                Grid.SetRow(headerBorder, 0);
                grid.Children.Add(headerBorder);
            }

            // Add row headers (1-10)
            for (int row = 0; row < size; row++)
            {
                var headerBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    ZIndex = 10
                };
                var header = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0))
                };
                headerBorder.Child = header;
                Grid.SetColumn(headerBorder, 0);
                Grid.SetRow(headerBorder, row + 1);
                grid.Children.Add(headerBorder);
            }

            // Add cells - только свое поле для размещения
            foreach (var cell in board.Cells)
            {
                var button = new Button
                {
                    Tag = cell,
                    Background = GetCellBackground(cell),
                    BorderBrush = cell.CellBorderBrush,
                    BorderThickness = new Avalonia.Thickness(cell.CellBorderThickness),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    Padding = new Avalonia.Thickness(0),
                    Margin = new Avalonia.Thickness(0)
                };

                button.Click += (s, e) =>
                {
                    if (gvm.SelectedShipSize.HasValue)
                    {
                        var success = gvm.PlaceShip(cell.Row, cell.Col, gvm.SelectedShipSize.Value, gvm.IsHorizontal);
                        if (success)
                        {
                            BuildPlacementBoard(); // Rebuild to update
                            // ShipManager сам уведомляет об изменениях через INotifyPropertyChanged
                        }
                    }
                };

                var textBlock = new TextBlock
                {
                    Text = GetCellDisplay(cell),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontSize = 14,
                    FontWeight = FontWeight.Bold
                };
                button.Content = textBlock;

                Grid.SetColumn(button, cell.Col + 1);
                Grid.SetRow(button, cell.Row + 1);
                grid.Children.Add(button);
                _cellControls[cell] = button;
                cell.PropertyChanged += (s, e) =>
                {
                    if (_cellControls.TryGetValue(cell, out var control) && control is Button btn)
                    {
                        btn.Background = GetCellBackground(cell);
                        btn.BorderBrush = cell.CellBorderBrush;
                        btn.BorderThickness = new Avalonia.Thickness(cell.CellBorderThickness);
                        if (btn.Content is TextBlock tb)
                        {
                            tb.Text = GetCellDisplay(cell);
                        }
                    }
                };
            }
        }

        private IBrush GetCellBackground(Models.BoardCell cell)
        {
            if (cell.IsSunk) return new SolidColorBrush(Color.FromRgb(220, 38, 38));
            if (cell.IsHit) return new SolidColorBrush(Color.FromRgb(252, 165, 165));
            if (cell.HasShip) return new SolidColorBrush(Color.FromRgb(147, 197, 253));
            return new SolidColorBrush(Color.FromRgb(219, 234, 254));
        }

        private string GetCellDisplay(Models.BoardCell cell)
        {
            if (cell.IsSunk) return "💥";
            if (cell.IsRevealed && cell.IsHit) return "✕";
            if (cell.IsRevealed && !cell.IsHit) return "·";
            if (cell.HasShip) return "⛵";
            return string.Empty;
        }
    }
}

