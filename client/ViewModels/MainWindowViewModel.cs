using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using client.Models;
using client.Services;

namespace client.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private object? _currentViewModel;

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set { _currentViewModel = value; OnPropertyChanged(); }
    }

    public GameServerClient GameClient { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel(GameServerClient client)
    {
        GameClient = client;

        var mainMenu = new MainMenuViewModel(client);
        mainMenu.RoomJoined += OnRoomJoined;
        CurrentViewModel = mainMenu;
    }

    private void OnRoomJoined(RoomInfo room)
    {
        var placement = new PlacementViewModel(GameClient, room, GameClient.DisplayName);
        placement.BackRequested += () =>
        {
            // вернуться в главное меню
            var menu = new MainMenuViewModel(GameClient);
            menu.RoomJoined += OnRoomJoined;
            CurrentViewModel = menu;
        };
        placement.GameStarted += isYourTurn =>
        {
            var myShips = placement.GetShipCoordinates();
            var gameVm = new GameViewModel(GameClient, room, isYourTurn, myShips);
            gameVm.ReturnToPlacementRequested += returnRoom =>
            {
                // Возврат в расстановку кораблей для новой игры
                var newPlacement = new PlacementViewModel(GameClient, returnRoom, GameClient.DisplayName);
                newPlacement.BackRequested += () =>
                {
                    var menu = new MainMenuViewModel(GameClient);
                    menu.RoomJoined += OnRoomJoined;
                    CurrentViewModel = menu;
                };
                newPlacement.GameStarted += isYourTurnAgain =>
                {
                    var myShipsAgain = newPlacement.GetShipCoordinates();
                    var gameVmAgain = new GameViewModel(GameClient, returnRoom, isYourTurnAgain, myShipsAgain);
                    CurrentViewModel = gameVmAgain;
                };
                CurrentViewModel = newPlacement;
            };
            CurrentViewModel = gameVm;
        };

        CurrentViewModel = placement;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

