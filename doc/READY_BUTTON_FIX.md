# Ready Button Implementation & Game State Flow

## Problem Statement
- Ready button ("готово") was not visible when both players joined the room
- Game didn't start when both players should click Ready
- Server wasn't transitioning game to ReadyToStart state

## Solution Overview

### 1. **Server-Side Changes** (WebSocketListener.cs)

#### Critical Fix: Message Type Case Sensitivity
```csharp
// BEFORE (BROKEN - case mismatch):
Type = "GameStateChanged"  // Client expects lowercase!

// AFTER (FIXED - matches client's case statement):
Type = "gamechanged"  // Lowercase is CRITICAL
```

**Why This Matters:** The client's NetworkService has a switch statement with case "gamechanged". If the server sends "GameStateChanged", the message is ignored completely.

#### Game Flow When Second Player Joins
```
Player1: Creates room → Room shown as "1/2" in lobby

Player2: Joins room → Server detects room is now FULL (2/2)

Server Action:
1. Player1 receives: {"Type":"gamechanged","Payload":{"state":"ReadyToStart"}}
2. Player2 receives: {"Type":"gamechanged","Payload":{"state":"ReadyToStart"}}

Result:
- Both players' GameViewModel.State becomes ReadyToStart
- StateToReadyVisibleConverter sees State == ReadyToStart
- Ready button becomes VISIBLE for both players
```

#### Server Code Location
**File:** `battle_of_sea/battle_of_sea/Network/WebSocketListener.cs`
**Method:** `HandleJoinRoom()`
**Lines:** 617-635

```csharp
Console.WriteLine($"[JoinRoom] Sending ReadyToStart to Player1: {game.Player1.Name}");
await player1Connection.SendAsync(new ServerMessage
{
    Type = "gamechanged",  // CRITICAL: lowercase
    Payload = new { state = "ReadyToStart" }
});

Console.WriteLine($"[JoinRoom] Sending ReadyToStart to Player2: {game.Player2.Name}");
await player2Connection.SendAsync(new ServerMessage
{
    Type = "gamechanged",  // CRITICAL: lowercase
    Payload = new { state = "ReadyToStart" }
});
```

---

### 2. **Client-Side Changes**

#### A. StateToReadyVisibleConverter (client/Converters/StateToVisibilityConverter.cs)
**Purpose:** Convert GameState.ReadyToStart to visibility for Ready button

```csharp
public class StateToReadyVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is GameState gameState)
        {
            bool isVisible = gameState == GameState.ReadyToStart;
            System.Console.WriteLine($"[StateToReadyVisibleConverter] State={gameState}, IsVisible={isVisible}");
            return isVisible;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
```

**Usage in XAML:** 
```xaml
<Button Content="✅ Готово"
        Command="{Binding ReadyCommand}"
        IsVisible="{Binding State, Converter={StaticResource StateToReadyVisibleConverter}}"
/>
```

#### B. GameViewModel State Property (client/ViewModels/GameViewModel.cs)
**Purpose:** Central game state management

```csharp
private GameState _state = GameState.WaitingForOpponent;

public GameState State
{
    get => _state;
    set
    {
        if ((_state == GameState.YouWin || _state == GameState.YouLose) && value != _state)
        {
            if (value != GameState.YouWin && value != GameState.YouLose)
                return;
        }
        _state = value; 
        Console.WriteLine($"[GameViewModel] State changed to: {value}");  // Logging
        OnPropertyChanged(); 
        OnPropertyChanged(nameof(StatusText)); 
        OnPropertyChanged(nameof(StatusBackground)); 
    }
}
```

**Key GameState Values:**
- `WaitingForOpponent`: Initial state, no opponent yet
- `ReadyToStart`: **Both players joined, ready button visible**
- `YourTurn`: Player can shoot
- `OpponentTurn`: Waiting for opponent
- `YouWin` / `YouLose`: Game ended

#### C. NetworkService Message Handling (client/Services/NetworkService.cs)
**Purpose:** Process incoming server messages

```csharp
// In HandleServerMessage method:
case "gamechanged":
case "gamestatechanged":
    Console.WriteLine($"[ListenForMessagesAsync] ✅ Received gamechanged/gamestatechanged message");
    HandleGameStateChangedMessage(json);
    break;

// In HandleGameStateChangedMessage:
private void HandleGameStateChangedMessage(string json)
{
    try
    {
        Console.WriteLine($"[HandleGameStateChangedMessage] ✅ Received game state change message");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("Payload", out var payload))
        {
            if (payload.TryGetProperty("state", out var stateProperty))
            {
                string state = stateProperty.GetString();
                Console.WriteLine($"[HandleGameStateChangedMessage] ✅ New state: {state}");
                
                var gameStateMessage = new GameStateMessage
                {
                    State = state
                };
                
                GameStateChanged?.Invoke(gameStateMessage);
                Console.WriteLine($"[HandleGameStateChangedMessage] Invoking GameStateChanged event");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[HandleGameStateChangedMessage] ❌ Error: {ex.Message}");
    }
}
```

#### D. GameView XAML (client/Views/GameView.axaml)
**Purpose:** UI layout with Ready button binding

```xaml
<converters:StateToReadyVisibleConverter x:Key="StateToReadyVisibleConverter"/>

<!-- ... existing content ... -->

<Button Content="✅ Готово"
        Command="{Binding ReadyCommand}"
        IsVisible="{Binding State, Converter={StaticResource StateToReadyVisibleConverter}}"
        Background="#10B981"
        Foreground="White"
        FontSize="14"
        FontWeight="Bold"
        Padding="12,8"
        CornerRadius="6"
        HorizontalAlignment="Center"
        Margin="0,8,0,0"
/>
```

---

## Complete Game Flow Diagram

```
STEP 1: Player1 Connects & Creates Room
├─ GameViewModel.State = WaitingForOpponent
├─ Ready button: HIDDEN (State != ReadyToStart)
└─ Server: Room created with 1/2 players

STEP 2: Player2 Connects
├─ GameViewModel.State = WaitingForOpponent
└─ Ready button: HIDDEN

STEP 3: Player2 Joins Room
├─ Server detects: Players = 2 (ROOM FULL!)
├─ Server sends gamechanged to Player1:
│  └─ {"Type":"gamechanged","Payload":{"state":"ReadyToStart"}}
├─ Server sends gamechanged to Player2:
│  └─ {"Type":"gamechanged","Payload":{"state":"ReadyToStart"}}

STEP 4: NetworkService Receives Messages
├─ Player1 Client:
│  ├─ HandleGameStateChangedMessage() called
│  ├─ Parses state = "ReadyToStart"
│  └─ Raises GameStateChanged event
├─ Player2 Client:
│  ├─ HandleGameStateChangedMessage() called
│  ├─ Parses state = "ReadyToStart"
│  └─ Raises GameStateChanged event

STEP 5: GameViewModel Updates State
├─ Player1: State = ReadyToStart
│  ├─ OnPropertyChanged() triggers
│  └─ UI binding updates
├─ Player2: State = ReadyToStart
│  ├─ OnPropertyChanged() triggers
│  └─ UI binding updates

STEP 6: StateToReadyVisibleConverter Runs
├─ Binding: IsVisible="{Binding State, Converter={...}}"
├─ Converter.Convert(ReadyToStart) = true
├─ Player1: Ready button VISIBLE ✅
└─ Player2: Ready button VISIBLE ✅

STEP 7: Both Players Click Ready Button
├─ Player1 clicks → sends PlayerReady message to server
└─ Player2 clicks → sends PlayerReady message to server

STEP 8: Server Processes Ready Clicks
├─ Server receives both PlayerReady messages
├─ Both have ready=true
├─ Server transitions game state to GameStarted
├─ Server sends gamechanged with state="YourTurn" or "OpponentTurn"

STEP 9: Game Starts
├─ Player1: State = YourTurn (if goes first)
├─ Player2: State = OpponentTurn
└─ Game board becomes interactive
```

---

## Debugging Checklist

### If Ready Button Still Doesn't Appear:

1. **Check Server Logs:**
   ```bash
   grep -i "gamechanged\|ReadyToStart" /tmp/server.log
   ```
   Should see:
   ```
   [JoinRoom] Sending ReadyToStart to Player1: [name]
   [JoinRoom] Sending ReadyToStart to Player2: [name]
   ```

2. **Check Client Console Logs:**
   - Look for: `[StateToReadyVisibleConverter]` messages
   - Look for: `[GameViewModel] State changed to: ReadyToStart`
   - Look for: `[HandleGameStateChangedMessage] ✅ Received gamechanged`

3. **Check Message Type (CRITICAL):**
   - Must be lowercase: `Type = "gamechanged"`
   - NOT `Type = "GameStateChanged"` (won't match case statement!)

4. **Check XAML Binding:**
   - Verify converter is registered: `StateToReadyVisibleConverter`
   - Verify binding: `IsVisible="{Binding State, Converter={...}}"`

### If Button Appears But Doesn't Work:

1. Check `ReadyCommand` is implemented in GameViewModel
2. Check `HandlePlayerReady` is implemented in server
3. Verify both players' ready flags are being set

---

## Critical Code Locations

| Component | File | Method | Lines |
|-----------|------|--------|-------|
| **Server sends gamechanged** | WebSocketListener.cs | HandleJoinRoom | 617-635 |
| **Client receives gamechanged** | NetworkService.cs | HandleServerMessage | 453-456 |
| **Client parses gamechanged** | NetworkService.cs | HandleGameStateChangedMessage | 610-651 |
| **State property triggers UI update** | GameViewModel.cs | State property | 17-35 |
| **Converter makes button visible** | StateToVisibilityConverter.cs | StateToReadyVisibleConverter | 34-48 |
| **XAML binds converter to button** | GameView.axaml | Button | Line 52 |

---

## Testing & Verification

### Python Test to Verify Server Messages
```python
# See: test_game_sequential.py
# Output should show:
# [Player2] ✅ ПОЛУЧЕНО ИЗМЕНЕНИЕ СОСТОЯНИЯ!
# [Player1] ✅ ПОЛУЧЕНО ИЗМЕНЕНИЕ СОСТОЯНИЯ!
```

### Build & Run
```bash
# Build server
cd /Users/masha/battle_of_sea/battle_of_sea && dotnet build

# Build client
cd /Users/masha/battle_of_sea/client && dotnet build BattleOfSea.csproj

# Run server
cd /Users/masha/battle_of_sea/battle_of_sea/battle_of_sea && dotnet run

# Run clients (two terminals)
cd /Users/masha/battle_of_sea/client && dotnet run BattleOfSea.csproj
```

---

## Summary of Fixes

✅ **Server:** Changed message Type from "GameStateChanged" → "gamechanged" (lowercase critical!)
✅ **Server:** Added logging to track game state transitions
✅ **Client:** Implemented StateToReadyVisibleConverter for button visibility
✅ **Client:** Added logging to GameViewModel.State property
✅ **Client:** Added case handlers in NetworkService for gamechanged messages
✅ **Client:** Added enhanced logging in HandleGameStateChangedMessage
✅ **XAML:** Bound Ready button's IsVisible to converter

## Result
Ready button now appears when:
1. Player1 creates room
2. Player2 joins room
3. Server sends gamechanged message to both
4. Both clients receive and process the message
5. GameViewModel.State changes to ReadyToStart
6. StateToReadyVisibleConverter returns true
7. Button becomes VISIBLE for both players

---

## Files Modified in This Session

1. `battle_of_sea/battle_of_sea/Network/WebSocketListener.cs`
   - Changed: Type = "gamechanged" (was "GameStateChanged")
   - Added: Comprehensive logging

2. `client/Converters/StateToVisibilityConverter.cs`
   - Added: StateToReadyVisibleConverter class
   - Added: Logging for debugging

3. `client/ViewModels/GameViewModel.cs`
   - Added: Console logging in State property setter

4. `client/Services/NetworkService.cs`
   - Added: case handlers for "gamechanged" and "gamestatechanged"
   - Enhanced: HandleGameStateChangedMessage with logging

5. `client/Views/GameView.axaml`
   - Added: StateToReadyVisibleConverter resource
   - Added: Ready button with visibility binding

