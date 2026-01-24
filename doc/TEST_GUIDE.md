# 🎮 UI CLIENT TEST GUIDE

## Quick Start

### Option 1: Automated Test (Recommended)
```bash
cd /Users/masha/battle_of_sea
./run_ui_test.sh
```

This will:
- ✅ Start the WebSocket server on port 5555
- ✅ Launch Client 1 (Room Creator)
- ✅ Launch Client 2 (Room Joiner)
- ✅ Capture all logs to `/tmp/battleoftsea_logs/`

### Option 2: Manual Test
```bash
# Terminal 1: Start server
cd /Users/masha/battle_of_sea/battle_of_sea/battle_of_sea
dotnet run --project battle_of_sea.csproj

# Terminal 2: Start client 1
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj

# Terminal 3: Start client 2
cd /Users/masha/battle_of_sea/client
dotnet run --project BattleOfSea.csproj
```

---

## Step-by-Step Manual Test

### Client 1 Actions:
1. **Connect Phase**
   - Enter username: `Player1`
   - Click "Connect"
   - Wait for rooms list to load

2. **Create Room**
   - Click "Create Room" button
   - Enter room name: `TestRoom`
   - Click "Create"
   - Wait for Player 2 to join

3. **Ready Phase**
   - When status shows "Ready to Start", click "Ready" button
   - Game should start

### Client 2 Actions:
1. **Connect Phase**
   - Enter username: `Player2`
   - Click "Connect"
   - Wait for rooms list to load

2. **Join Room**
   - Select "TestRoom" from the list
   - Click "Join Room"
   - Wait for Player 1 to be ready

3. **Ready Phase**
   - When status shows "Ready to Start", click "Ready" button
   - Game should start

---

## Key Logs to Monitor

### Expected LOG FLOW:

#### 1️⃣ Client 1 Logs (Creating Room):
```
[DEBUG][GameVM] RoomId initialized = '00000000-0000-0000-0000-000000000001'
[DEBUG] OnRoomJoinRequested: room.Id=00000000-0000-0000-0000-000000000001
```

#### 2️⃣ Client 2 Logs (Joining Room):
```
[DEBUG] JoinRoomAsync called
[DEBUG] Set _currentRoomId = '00000000-0000-0000-0000-000000000001'
[DEBUG][GameVM] RoomId initialized = '00000000-0000-0000-0000-000000000001'
```

#### 3️⃣ Server Logs (Both Players Ready):
```
[DEBUG] HandlePlayerReady START
[DEBUG] Set Player1Ready=true
[DEBUG] After update: Player1Ready=True, Player2Ready=True
[DEBUG] game.BothPlayersReady=True
[DEBUG] ✅ BOTH PLAYERS READY - SENDING GAMESTART
[DEBUG] ✅ GameStart sent to Player1
[DEBUG] ✅ GameStart sent to Player2
```

#### 4️⃣ Client Logs (Game Started):
```
[DEBUG] OnGameStateChanged START
[DEBUG] message.State=YourTurn
[DEBUG] ✅ Parsed successfully to: YourTurn
[DEBUG] Current State after assignment: YourTurn
```

---

## Real-Time Log Monitoring

While test is running, in separate terminals:

```bash
# Watch server logs
tail -f /tmp/battleoftsea_logs/server.log

# Watch client 1 logs
tail -f /tmp/battleoftsea_logs/client1.log

# Watch client 2 logs
tail -f /tmp/battleoftsea_logs/client2.log

# Search for specific issues
grep -i "debug" /tmp/battleoftsea_logs/server.log
grep -i "error" /tmp/battleoftsea_logs/server.log
```

---

## Analyzing Test Results

After test is complete, run:
```bash
./analyze_logs.sh
```

This will show:
- ✅ GameViewModel initialization with roomId
- ✅ SendPlayerReady with correct roomId  
- ✅ Server receiving both PlayerReady messages
- ✅ Server sending GameStart to both clients
- ✅ Clients receiving and processing GameStart

---

## What We're Testing

| Component | What to Verify |
|-----------|-----------------|
| **NetworkService.JoinRoomAsync** | Sets `_currentRoomId` from `room.Id` before sending message |
| **GameViewModel Constructor** | Initializes `_roomId` from `room?.Id` parameter |
| **MainWindow.OnRoomJoinRequested** | Passes Room object with valid Id to GameViewModel |
| **LobbyViewModel.OnJoinRoomResult** | Finds and passes room with correct Id |
| **SendPlayerReadyAsync** | Uses `_currentRoomId` when roomId parameter is empty |
| **Server.HandlePlayerReady** | Receives roomId, finds game, updates ready flags, sends GameStart |
| **Client.HandleGameStateChangedMessage** | Receives GameStart and parses state correctly |
| **GameViewModel.OnGameStateChanged** | Sets State property to YourTurn/OpponentTurn |

---

## Troubleshooting

### Issue: "Connection failed"
- Verify server is running: `lsof -i :5555`
- Check server logs: `cat /tmp/battleoftsea_logs/server.log | tail -20`

### Issue: "roomId is empty"
- Check GameViewModel logs for `RoomId initialized = ''`
- Check LobbyViewModel logs for room lookup failures
- Ensure room.Id is populated in OnRoomsListUpdated

### Issue: "GameStart not received"
- Check server logs for `HandlePlayerReady START`
- Verify both players show ready
- Check `game.BothPlayersReady=True` in logs

### Issue: "Game doesn't start on UI"
- Check client logs for `OnGameStateChanged START`
- Verify state is parsing correctly: `Parsed successfully to: YourTurn`
- Check State property assignment in GameViewModel

---

## Files Modified

✅ `client/Services/NetworkService.cs` - Added DEBUG logging to JoinRoomAsync and SendPlayerReadyAsync
✅ `client/ViewModels/GameViewModel.cs` - Added DEBUG logging to constructor and OnGameStateChanged
✅ `client/MainWindow.axaml.cs` - Added DEBUG logging when creating GameViewModel
✅ `client/ViewModels/LobbyViewModel.cs` - Fixed OnJoinRoomResult to search by room.Id
✅ `battle_of_sea/Network/WebSocketListener.cs` - Added comprehensive DEBUG logging to HandlePlayerReady

---

## Success Criteria ✅

- [ ] Both clients connect to server
- [ ] Client 1 creates room with valid UUID
- [ ] Client 2 joins room and gets same UUID
- [ ] Both clients show "Ready to Start" status
- [ ] Both clients send PlayerReady with valid roomId
- [ ] Server receives both PlayerReady messages
- [ ] Server sends GameStart to both clients
- [ ] Both clients receive GameStart
- [ ] Game transitions to YourTurn/OpponentTurn
- [ ] Status text updates on UI

---

## Notes

- All logs are timestamped
- DEBUG logs have `[DEBUG]` prefix for easy filtering
- Server logs are saved to `/tmp/battleoftsea_logs/server.log`
- Client logs are saved to `/tmp/battleoftsea_logs/client1.log` and `client2.log`
- Use `grep -i debug` to find all debug messages
- Use `grep ERROR` to find any errors

Good luck! 🍀
