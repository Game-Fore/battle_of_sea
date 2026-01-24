# 🎮 Player Ready System Implementation

## Overview
Added "Ready" button system that allows players to signal they are ready to start a game after placing their ships. When both players are ready, the game automatically starts with the first move going to the room creator.

## Features Implemented

### 1. **New Game State**
- Added `ReadyToStart` state to `GameState` enum
- Displays: "✅ Оба готовы! Нажмите 'Начать игру'"
- Blue status bar (#3182f6) with white text

### 2. **UI Changes**
- **Ship Placement View**: Added "✅ Готов начать игру" button
  - Green button (#10b981) with white text
  - Shows when `ShipsPlaced == true`
  - Disabled until player has placed all ships

### 3. **Network Protocol**
- **Client → Server**: `PlayerReady` message with `roomId` and `userId`
- **Server → Client**: `GameStart` message with:
  - `isYourTurn` - indicates if this player goes first
  - `firstPlayer` - ID of player who goes first (room creator)

### 4. **Server-Side Logic**
- Tracks player ready status
- When a player sends `PlayerReady`:
  - Verifies player is in an active game
  - Sends `GameStart` message to both players
  - Sets first turn to room creator

### 5. **Client-Side Logic**
- **GameViewModel.SendPlayerReadyAsync()**:
  - Sends `PlayerReady` message to server
  - Triggered by `ReadyCommand` button

- **NetworkService.HandleGameStartMessage()**:
  - Parses `GameStart` response
  - Converts to `GameStateMessage` with "YourTurn" or "OpponentTurn" state
  - Invokes `GameStateChanged` event

- **GameViewModel.OnGameStateChanged()**:
  - Parses state string using `Enum.TryParse`
  - Updates UI to show "Ваш ход" or "Ход соперника"

## Modified Files

| File | Changes |
|------|---------|
| `client/Models/GameState.cs` | Added `ReadyToStart` state |
| `client/Models/NetworkMessage.cs` | Changed GameStateMessage.State from enum to string |
| `client/ViewModels/GameViewModel.cs` | Added ReadyCommand, SendPlayerReadyAsync(), updated OnGameStateChanged() |
| `client/Services/INetworkService.cs` | Added SendPlayerReadyAsync() method signature |
| `client/Services/NetworkService.cs` | Implemented SendPlayerReadyAsync(), HandleGameStartMessage() |
| `client/Services/GameServerClient.cs` | Updated GameStateMessage parsing to use string state |
| `client/Services/MockNetworkService.cs` | Added SendPlayerReadyAsync() stub |
| `client/Views/ShipPlacementView.axaml` | Added "Готов" button |
| `battle_of_sea/Network/WebSocketListener.cs` | Added HandlePlayerReady() handler, "playerready" case |

## Game Flow

1. **Player 1 Creates Room** → Appears in Player 2's lobby
2. **Player 2 Joins Room** → Room shows 2/2 players
3. **Both Place Ships** → "Начать игру" button appears
4. **Player 1 Clicks "✅ Готов"** → Sends PlayerReady to server
   - Server sends GameStart to Player 1: isYourTurn=true
   - Server sends GameStart to Player 2: isYourTurn=false
5. **Player 2 Clicks "✅ Готов"** → Game officially starts
   - Both see game board ready
   - Player 1 can click on enemy board to shoot
   - Player 2 sees "Ход соперника" (waiting)

## Status Transitions

```
WaitingForOpponent
    ↓ (both ships placed)
ReadyToStart (show "✅ Готов" button)
    ↓ (both click Ready)
YourTurn / OpponentTurn
    ↓ (game progresses)
YouWin / YouLose
```

## Testing Steps

1. **Start Server**: `cd battle_of_sea/battle_of_sea/battle_of_sea && dotnet run`
2. **Start Client 1**: `cd client && dotnet run --project BattleOfSea.csproj`
3. **Start Client 2**: `cd client && dotnet run --project BattleOfSea.csproj` (different window)

### Test Scenario:
1. Client 1: Create room
2. Client 2: Join room (should see 2/2)
3. Both: Place ships (all corners)
4. Both: Click "Начать игру"
5. Both: Should see "Ваш ход" or "Ход соперника"
6. Client 1 (creator) should have "Ваш ход" - can shoot
7. Client 2 should see "Ход соперника" - waiting

## Build Status
✅ Client: 0 errors, 1 warning (safe)
✅ Server: 0 errors

## Key Implementation Details

### GameStart Message Format
```json
{
  "Type": "GameStart",
  "Payload": {
    "success": true,
    "firstPlayer": "<player1_id>",
    "isYourTurn": true/false
  }
}
```

### State Enum → String Conversion
The `GameStateMessage.State` was changed to `string` to allow flexibility in state management. When received from server, it's parsed using:
```csharp
if (Enum.TryParse<GameState>(message.State, true, out var newState))
{
    State = newState;
}
```

## Future Improvements
- Add countdown timer before forced game start
- Add "Cancel" button to leave ready state
- Add spectator mode
- Add forfeit/surrender button during game
