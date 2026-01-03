using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Linq;
using System.Collections.Specialized;

namespace BattleOfSea.Views
{
    public partial class BoardView : UserControl
    {
        private ViewModels.BoardViewModel? VM => DataContext as ViewModels.BoardViewModel;
        private const int CellSize = 30;
        private const int HeaderSize = 28;
        private System.Collections.Generic.Dictionary<Models.BoardCell, Control> _ownCellControls = new();
        private System.Collections.Generic.Dictionary<Models.BoardCell, Control> _enemyCellControls = new();

        public BoardView()
        {
            InitializeComponent();
            // Do not set DataContext here; expect parent (GameView) to provide GameViewModel
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (IsInitialized)
            {
                BuildBoards();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            BuildBoards();
        }

        private void BuildBoards()
        {
            // Clear previous subscriptions
            foreach (var cell in _ownCellControls.Keys)
            {
                cell.PropertyChanged -= OnOwnCellPropertyChanged;
            }
            foreach (var cell in _enemyCellControls.Keys)
            {
                cell.PropertyChanged -= OnEnemyCellPropertyChanged;
            }
            _ownCellControls.Clear();
            _enemyCellControls.Clear();

            if (DataContext is ViewModels.GameViewModel gvm)
            {
                BuildBoard(OwnBoardGrid, gvm.Own, true);
                BuildBoard(EnemyBoardGrid, gvm.Enemy, false);
            }
            else if (DataContext is ViewModels.BoardViewModel bvm)
            {
                BuildBoard(OwnBoardGrid, bvm.Own, true);
                BuildBoard(EnemyBoardGrid, bvm.Enemy, false);
            }
        }

        private void OnOwnCellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Models.BoardCell cell && _ownCellControls.TryGetValue(cell, out var control))
            {
                UpdateOwnCellControl(control, cell);
            }
        }

        private void OnEnemyCellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Models.BoardCell cell && _enemyCellControls.TryGetValue(cell, out var control))
            {
                UpdateEnemyCellControl(control, cell);
            }
        }

        private void UpdateOwnCellControl(Control control, Models.BoardCell cell)
        {
            if (control is Border border && border.Child is TextBlock textBlock)
            {
                border.Background = GetOwnCellBackground(cell);
                border.BorderBrush = cell.CellBorderBrush;
                border.BorderThickness = new Avalonia.Thickness(cell.CellBorderThickness);
                textBlock.Text = GetOwnCellDisplay(cell);
            }
        }

        private void UpdateEnemyCellControl(Control control, Models.BoardCell cell)
        {
            if (control is Border border)
            {
                border.Background = GetEnemyCellBackground(cell);
                border.BorderBrush = cell.CellBorderBrush;
                border.BorderThickness = new Avalonia.Thickness(cell.CellBorderThickness);
                border.IsHitTestVisible = !cell.IsRevealed; // Отключаем клики для открытых ячеек
                if (border.Child is TextBlock textBlock)
                {
                    textBlock.Text = cell.Display;
                }
            }
        }

        private void BuildBoard(Grid grid, Models.Board board, bool isOwnBoard)
        {
            grid.Children.Clear();
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            int size = board.Size;

            // Create column definitions: header + size columns (all same size)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Avalonia.Controls.GridLength(HeaderSize, Avalonia.Controls.GridUnitType.Pixel) });
            for (int i = 0; i < size; i++)
            {
                // Ensure all columns have exactly the same width
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Avalonia.Controls.GridLength(CellSize, Avalonia.Controls.GridUnitType.Pixel), MinWidth = CellSize, MaxWidth = CellSize });
            }

            // Create row definitions: header + size rows (all same size)
            grid.RowDefinitions.Add(new RowDefinition { Height = new Avalonia.Controls.GridLength(HeaderSize, Avalonia.Controls.GridUnitType.Pixel) });
            for (int i = 0; i < size; i++)
            {
                // Ensure all rows have exactly the same height
                grid.RowDefinitions.Add(new RowDefinition { Height = new Avalonia.Controls.GridLength(CellSize, Avalonia.Controls.GridUnitType.Pixel), MinHeight = CellSize, MaxHeight = CellSize });
            }

            // Add column headers (A-J)
            for (int col = 0; col < size; col++)
            {
                var headerBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), // Transparent
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    ZIndex = 10 // Ensure headers are above cells
                };
                var header = new TextBlock
                {
                    Text = ((char)('A' + col)).ToString(),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)) // Black
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
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), // Transparent
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    ZIndex = 10 // Ensure headers are above cells
                };
                var header = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)) // Black
                };
                headerBorder.Child = header;
                Grid.SetColumn(headerBorder, 0);
                Grid.SetRow(headerBorder, row + 1);
                grid.Children.Add(headerBorder);
            }

            // Add cells
            foreach (var cell in board.Cells)
            {
                Control cellControl;
                
                if (isOwnBoard)
                {
                    // Мое поле - Button для размещения кораблей или Border для игры
                    if (DataContext is ViewModels.GameViewModel gvm && !gvm.ShipsPlaced)
                    {
                        // В режиме размещения - Button
                        var button = new Button
                        {
                            Tag = cell,
                            Background = GetOwnCellBackground(cell),
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
                                gvm.PlaceShipAtCell(cell);
                                BuildBoard(grid, board, isOwnBoard); // Rebuild to update
                            }
                        };
                        
                        var textBlock = new TextBlock
                        {
                            Text = GetOwnCellDisplay(cell),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            FontSize = 14,
                            FontWeight = FontWeight.Bold
                        };
                        button.Content = textBlock;
                        cellControl = button;
                    }
                    else
                    {
                        // В режиме игры - Border
                        var border = new Border
                        {
                            BorderBrush = cell.CellBorderBrush,
                            BorderThickness = new Avalonia.Thickness(cell.CellBorderThickness),
                            Background = GetOwnCellBackground(cell),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                            Margin = new Avalonia.Thickness(0)
                        };
                        
                        var textBlock = new TextBlock
                        {
                            Text = GetOwnCellDisplay(cell),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            FontSize = 14,
                            FontWeight = FontWeight.Bold
                        };
                        border.Child = textBlock;
                        cellControl = border;
                    }
                    _ownCellControls[cell] = cellControl;
                    cell.PropertyChanged += OnOwnCellPropertyChanged;
                }
                else
                {
                    // Поле противника - Border с обработчиком клика (без стандартных эффектов Button)
                    var border = new Border
                    {
                        Tag = cell,
                        Background = GetEnemyCellBackground(cell),
                        BorderBrush = cell.CellBorderBrush,
                        BorderThickness = new Avalonia.Thickness(cell.CellBorderThickness),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                        Margin = new Avalonia.Thickness(0),
                        IsHitTestVisible = !cell.IsRevealed // Отключаем клики для открытых ячеек
                    };
                    
                    var textBlock = new TextBlock
                    {
                        Text = cell.Display,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        FontSize = 14,
                        FontWeight = FontWeight.Bold
                    };
                    border.Child = textBlock;
                    
                    // Обработчик клика через PointerPressed
                    border.PointerPressed += (s, e) =>
                    {
                        if (s is Border b && b.Tag is Models.BoardCell clickedCell && !clickedCell.IsRevealed)
                        {
                            EnemyCell_Click(border, clickedCell);
                        }
                    };
                    
                    cellControl = border;
                    _enemyCellControls[cell] = cellControl;
                    cell.PropertyChanged += OnEnemyCellPropertyChanged;
                }

                Grid.SetColumn(cellControl, cell.Col + 1);
                Grid.SetRow(cellControl, cell.Row + 1);
                grid.Children.Add(cellControl);
            }
        }

        private IBrush GetOwnCellBackground(Models.BoardCell cell)
        {
            // Мое поле - светло-голубой фон, корабли видны
            if (cell.IsSunk) return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Красный для потопленных
            if (cell.IsHit) return new SolidColorBrush(Color.FromRgb(252, 165, 165)); // Светло-красный для попаданий
            if (cell.HasShip) return new SolidColorBrush(Color.FromRgb(147, 197, 253)); // Голубой для кораблей
            return new SolidColorBrush(Color.FromRgb(219, 234, 254)); // Очень светло-голубой для пустых
        }

        private string GetOwnCellDisplay(Models.BoardCell cell)
        {
            // На моем поле показываем корабли и результаты выстрелов противника
            if (cell.IsSunk) return "💥";
            if (cell.IsRevealed && cell.IsHit) return "✕"; // Попадание - крестик
            if (cell.IsRevealed && !cell.IsHit) return "·"; // Промах - точка
            if (cell.HasShip) return "⛵"; // Корабль виден
            return string.Empty;
        }

        private IBrush GetEnemyCellBackground(Models.BoardCell cell)
        {
            // Поле противника - такой же фон как мое поле, но корабли не видны
            // Все неоткрытые ячейки должны быть одного цвета
            if (cell.IsSunk) return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Красный для потопленных
            if (cell.IsHit) return new SolidColorBrush(Color.FromRgb(252, 165, 165)); // Светло-красный для попаданий
            if (cell.IsRevealed && !cell.IsHit) return new SolidColorBrush(Color.FromRgb(229, 231, 235)); // Серый для промахов
            // Все неоткрытые ячейки - одинаковый светло-голубой цвет
            return new SolidColorBrush(Color.FromRgb(219, 234, 254)); // Очень светло-голубой для всех неоткрытых
        }

        private async void EnemyCell_Click(object? sender, Models.BoardCell cell)
        {
            if (cell != null && !cell.IsRevealed)
            {
                // If the view is hosted inside GameView, call its Shoot handler
                if (DataContext is ViewModels.GameViewModel gvm)
                {
                    await gvm.ShootAt(cell);
                }
                else
                {
                    VM?.EnemyCellClick(cell);
                }
            }
        }
    }
}
