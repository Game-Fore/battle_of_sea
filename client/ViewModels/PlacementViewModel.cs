using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Text.Json;
using client.Models;
using client.Services;
using client.Utils;

namespace client.ViewModels;

public class PlacementViewModel : INotifyPropertyChanged
{
    private readonly GameServerClient _client;
    private string _displayName = "Player";
    private string _status = "Расставьте корабли на своём поле";
    private bool _isHorizontal = true;
    private int _shipsToPlace4 = 1;
    private int _shipsToPlace3 = 2;
    private int _shipsToPlace2 = 3;
    private int _shipsToPlace1 = 4;
    private bool _allShipsPlaced;
    private bool _isReady;

    public RoomInfo Room { get; }
    public string DisplayName => _displayName;

    public ObservableCollection<CellViewModel> Cells { get; } = new();

    public bool IsHorizontal
    {
        get => _isHorizontal;
        set { _isHorizontal = value; OnPropertyChanged(); }
    }

    public int ShipsToPlace4
    {
        get => _shipsToPlace4;
        set { _shipsToPlace4 = value; OnPropertyChanged(); }
    }

    public int ShipsToPlace3
    {
        get => _shipsToPlace3;
        set { _shipsToPlace3 = value; OnPropertyChanged(); }
    }

    public int ShipsToPlace2
    {
        get => _shipsToPlace2;
        set { _shipsToPlace2 = value; OnPropertyChanged(); }
    }

    public int ShipsToPlace1
    {
        get => _shipsToPlace1;
        set { _shipsToPlace1 = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public ICommand BackToMenuCommand { get; }
    public ICommand ToggleOrientationCommand { get; }
    public ICommand CellClickCommand { get; }
    public ICommand ReadyCommand { get; }

    public bool CanPressReady => _allShipsPlaced && !_isReady;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event System.Action? BackRequested;
    public event System.Action<bool>? GameStarted;

    public PlacementViewModel(GameServerClient client, RoomInfo room, string displayName = "Player")
    {
        _client = client;
        Room = room;
        _displayName = displayName;
        Room.MyPlayerName = displayName;

        // инициализируем поле 10x10
        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                Cells.Add(new CellViewModel(x, y));
            }
        }

        BackToMenuCommand = new RelayCommand(async _ =>
        {
            // уведомим сервер, что выходим из комнаты
            await _client.SendAsync("leaveroom", new { });
            BackRequested?.Invoke();
        });

        ToggleOrientationCommand = new RelayCommand(_ =>
        {
            IsHorizontal = !IsHorizontal;
        });

        CellClickCommand = new RelayCommand(param =>
        {
            if (param is CellViewModel cell)
            {
                TryPlaceShipAt(cell.X, cell.Y);
            }
        });

        // Присваиваем ссылку на команду каждой ячейке, чтобы биндинг в XAML был простым
        foreach (var cell in Cells)
        {
            cell.CellClickCommand = CellClickCommand;
        }

        ReadyCommand = new RelayCommand(async _ => await SendReadyAsync(), _ => CanPressReady);

        _client.MessageReceived += OnServerMessage;
    }

    private void OnServerMessage(string type, JsonElement payload)
    {
        if (string.Equals(type, "GameStart", StringComparison.OrdinalIgnoreCase))
        {
            var isYourTurn = payload.TryGetProperty("isYourTurn", out var turnProp) && turnProp.GetBoolean();
            Status = "Бой начался!";
            GameStarted?.Invoke(isYourTurn);
        }
    }

    private void TryPlaceShipAt(int x, int y)
    {
        var size = GetCurrentShipSize();
        if (size == 0)
            return;

        // проверяем, можем ли разместить корабль
        var cellsToOccupy = new List<CellViewModel>();
        for (int i = 0; i < size; i++)
        {
            var cx = _isHorizontal ? x + i : x;
            var cy = _isHorizontal ? y : y + i;

            if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10)
                return;

            var cell = GetCell(cx, cy);
            if (cell == null || cell.HasShip)
                return;

            // проверяем соседей (включая диагональные) – не должно быть других кораблей
            for (int ny = cy - 1; ny <= cy + 1; ny++)
            {
                for (int nx = cx - 1; nx <= cx + 1; nx++)
                {
                    if (nx < 0 || nx >= 10 || ny < 0 || ny >= 10)
                        continue;
                    var neighbor = GetCell(nx, ny);
                    if (neighbor != null && neighbor.HasShip)
                        return;
                }
            }

            cellsToOccupy.Add(cell);
        }

        // размещаем
        foreach (var c in cellsToOccupy)
        {
            c.HasShip = true;
        }

        DecrementShipCounter(size);

        if (ShipsToPlace1 == 0 && ShipsToPlace2 == 0 && ShipsToPlace3 == 0 && ShipsToPlace4 == 0)
        {
            _allShipsPlaced = true;
            Status = "Все корабли расставлены. Нажмите 'Готов к бою'.";
            OnPropertyChanged(nameof(CanPressReady));
            if (ReadyCommand is client.Utils.RelayCommand rc)
                rc.RaiseCanExecuteChanged();
        }
    }

    private int GetCurrentShipSize()
    {
        if (ShipsToPlace4 > 0) return 4;
        if (ShipsToPlace3 > 0) return 3;
        if (ShipsToPlace2 > 0) return 2;
        if (ShipsToPlace1 > 0) return 1;
        return 0;
    }

    private void DecrementShipCounter(int size)
    {
        switch (size)
        {
            case 4: ShipsToPlace4--; break;
            case 3: ShipsToPlace3--; break;
            case 2: ShipsToPlace2--; break;
            case 1: ShipsToPlace1--; break;
        }
        OnPropertyChanged(nameof(CanPressReady));
        if (ReadyCommand is client.Utils.RelayCommand rc)
            rc.RaiseCanExecuteChanged();
    }

    private CellViewModel? GetCell(int x, int y)
    {
        foreach (var c in Cells)
        {
            if (c.X == x && c.Y == y)
                return c;
        }
        return null;
    }

    /// <summary>Список кораблей для отправки на сервер: x, y, size, horizontal.</summary>
    public IReadOnlyList<(int x, int y, int size, bool horizontal)> GetShips()
    {
        var set = new HashSet<(int x, int y)>();
        foreach (var c in Cells)
            if (c.HasShip) set.Add((c.X, c.Y));

        var list = new List<(int x, int y, int size, bool horizontal)>();
        while (set.Count > 0)
        {
            var p = set.First();
            bool horizontal;
            int size;
            if (set.Contains((p.x + 1, p.y)))
            {
                horizontal = true;
                size = 0;
                for (int xx = p.x; xx < 10 && set.Contains((xx, p.y)); xx++) { size++; set.Remove((xx, p.y)); }
            }
            else
            {
                horizontal = false;
                size = 0;
                for (int yy = p.y; yy < 10 && set.Contains((p.x, yy)); yy++) { size++; set.Remove((p.x, yy)); }
            }
            list.Add((p.x, p.y, size, horizontal));
        }
        return list;
    }

    public IReadOnlyList<(int x, int y)> GetShipCoordinates()
    {
        var list = new List<(int x, int y)>();
        foreach (var c in Cells)
            if (c.HasShip) list.Add((c.X, c.Y));
        return list;
    }

    private async Task SendReadyAsync()
    {
        if (!_allShipsPlaced || _isReady)
            return;

        _isReady = true;
        Status = "Вы готовы. Ожидаем соперника...";
        OnPropertyChanged(nameof(CanPressReady));

        var ships = GetShips().Select(s => new { x = s.x, y = s.y, size = s.size, horizontal = s.horizontal }).ToList();
        await _client.SendAsync("shipplacement", new { roomId = Room.Id, ships });
        await _client.SendAsync("playerready", new { roomId = Room.Id });
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class CellViewModel : INotifyPropertyChanged
{
    private bool _hasShip;

    public int X { get; }
    public int Y { get; }

    public bool HasShip
    {
        get => _hasShip;
        set { _hasShip = value; OnPropertyChanged(); }
    }

    // Команда будет задаваться из PlacementViewModel
    public ICommand? CellClickCommand { get; set; }

    public CellViewModel(int x, int y)
    {
        X = x;
        Y = y;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

