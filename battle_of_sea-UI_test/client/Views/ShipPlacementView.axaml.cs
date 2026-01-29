// Экран расстановки
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System;
using System.Collections.Generic;

namespace BattleOfSea.Views
{
    // Разметка поля
    public partial class ShipPlacementView : UserControl
    {
        private const int CellSize = 30;
        private const int HeaderSize = 28;

        private Dictionary<Models.BoardCell, Border> _cellControls = new();

        // Конструктор представления расстановки (публичный)
        public ShipPlacementView()
        {
            InitializeComponent();
        }

        // Обработчик завершения инициализации (защищенный метод переопределения)
        protected override void OnInitialized()
        {
            base.OnInitialized();
            BuildPlacementBoard();
        }

        // Построить поле расстановки (приватный метод)
        private void BuildPlacementBoard()
        {
            if (DataContext is ViewModels.GameViewModel gvm)
                BuildBoard(PlacementBoardGrid, gvm.Own, gvm);
        }

        // Обработчик изменения контекста данных (защищенный метод переопределения)
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (IsInitialized)
                BuildPlacementBoard();
        }

        // Построить сетку (приватный метод)
        private void BuildBoard(Grid grid, Models.Board board, ViewModels.GameViewModel gvm)
        {
            grid.Children.Clear();
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();
            _cellControls.Clear();

            int size = board.Size;
            int totalSize = HeaderSize + size * CellSize;

            grid.Width = totalSize;
            grid.Height = totalSize;
            grid.MinWidth = totalSize;
            grid.MinHeight = totalSize;
            grid.MaxWidth = totalSize;
            grid.MaxHeight = totalSize;

            grid.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            grid.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;

            // === СТОЛБЦЫ ===
            grid.ColumnDefinitions.Add(
                new ColumnDefinition(new GridLength(HeaderSize, GridUnitType.Pixel)));

            for (int i = 0; i < size; i++)
            {
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(new GridLength(CellSize, GridUnitType.Pixel)));
            }

            // === СТРОКИ ===
            grid.RowDefinitions.Add(
                new RowDefinition(new GridLength(HeaderSize, GridUnitType.Pixel)));

            for (int i = 0; i < size; i++)
            {
                grid.RowDefinitions.Add(
                    new RowDefinition(new GridLength(CellSize, GridUnitType.Pixel)));
            }

            // === ЗАГОЛОВКИ СТОЛБЦОВ ===
            for (int col = 0; col < size; col++)
            {
                var text = new TextBlock
                {
                    Text = ((char)('A' + col)).ToString(),
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                Grid.SetRow(text, 0);
                Grid.SetColumn(text, col + 1);
                grid.Children.Add(text);
            }

            // === ЗАГОЛОВКИ СТРОК ===
            for (int row = 0; row < size; row++)
            {
                var text = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                Grid.SetRow(text, row + 1);
                Grid.SetColumn(text, 0);
                grid.Children.Add(text);
            }

            // === ЯЧЕЙКИ ===
            foreach (var cell in board.Cells)
            {
                var border = new Border
                {
                    Background = GetCellBackground(cell),
                    BorderBrush = cell.CellBorderBrush,
                    BorderThickness = new Avalonia.Thickness(cell.CellBorderThickness),
                    Width = CellSize,
                    Height = CellSize,
                    Padding = new Avalonia.Thickness(0),
                    Margin = new Avalonia.Thickness(0),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                };

                var text = new TextBlock
                {
                    Text = GetCellDisplay(cell),
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                border.Child = text;

                // Обработчик нажатия на ячейку
                border.PointerPressed += (s, e) =>
                {
                    var point = e.GetCurrentPoint(border);

                    // Правая кнопка мыши - удалить корабль
                    if (point.Properties.IsRightButtonPressed && cell.HasShip)
                    {
                        gvm.RemoveShip(cell.Row, cell.Col);
                        BuildPlacementBoard();
                        e.Handled = true;
                        return;
                    }

                    // Левая кнопка мыши - разместить корабль
                    if (point.Properties.IsLeftButtonPressed &&
                        gvm.SelectedShipSize.HasValue)
                    {
                        if (gvm.PlaceShip(
                            cell.Row,
                            cell.Col,
                            gvm.SelectedShipSize.Value,
                            gvm.IsHorizontal))
                        {
                            BuildPlacementBoard();
                        }
                    }
                };

                Grid.SetRow(border, cell.Row + 1);
                Grid.SetColumn(border, cell.Col + 1);
                grid.Children.Add(border);

                _cellControls[cell] = border;
            }
        }

        // Цвет ячейки (приватный метод)
        private IBrush GetCellBackground(Models.BoardCell cell)
        {
            if (cell.IsSunk) return Brushes.Red;
            if (cell.IsHit) return Brushes.OrangeRed;
            if (cell.HasShip) return Brushes.LightBlue;
            return Brushes.AliceBlue;
        }

        // Текст ячейки (приватный метод)
        private string GetCellDisplay(Models.BoardCell cell)
        {
            if (cell.IsSunk) return "💥";
            if (cell.HasShip) return "⛵";
            return string.Empty;
        }
    }
}