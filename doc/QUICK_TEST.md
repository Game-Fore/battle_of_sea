# 🎮 QUICK REFERENCE - UI TEST

## 📋 One-Command Test Start
```bash
cd /Users/masha/battle_of_sea && ./run_ui_test.sh
```

## 🖥️ What Happens Automatically
- ✅ Server starts on port 5555
- ✅ Client 1 launches (Room Creator)
- ✅ Client 2 launches (Room Joiner)
- ✅ All logs saved to `/tmp/battleoftsea_logs/`

---

## 🎯 MANUAL UI STEPS

### Client 1:
```
1. Username: Player1
2. Click Connect
3. Click "Create Room"
4. Room name: TestRoom
5. Click "Ready" when opponent joins
```

### Client 2:
```
1. Username: Player2
2. Click Connect
3. Select TestRoom from list
4. Click "Join Room"
5. Click "Ready" after Player1
```

---

## 📊 MONITOR DURING TEST

```bash
# Terminal A: Main test
./run_ui_test.sh

# Terminal B: Real-time events (optional)
./monitor_test.sh

# Terminal C: Real-time server logs (optional)
tail -f /tmp/battleoftsea_logs/server.log | grep DEBUG

# Terminal D: Real-time client 1 logs (optional)
tail -f /tmp/battleoftsea_logs/client1.log | grep DEBUG
```

---

## ✅ AFTER TEST COMPLETES

```bash
./analyze_logs.sh
```

Shows:
- ✅ roomId initialized correctly
- ✅ PlayerReady sent with correct roomId
- ✅ Server received both PlayerReady
- ✅ Server sent GameStart
- ✅ Clients received GameStart

---

## 🔍 SEARCH LOGS FOR ISSUES

```bash
# All debug messages
grep DEBUG /tmp/battleoftsea_logs/server.log

# All errors
grep ERROR /tmp/battleoftsea_logs/server.log

# PlayerReady messages
grep PlayerReady /tmp/battleoftsea_logs/server.log

# GameStart messages
grep GameStart /tmp/battleoftsea_logs/server.log

# Room ID in client
grep "RoomId initialized" /tmp/battleoftsea_logs/client1.log

# Room ID in network
grep "Set _currentRoomId" /tmp/battleoftsea_logs/client1.log
```

---

## 🎯 SUCCESS INDICATORS

Look for these patterns in logs:

✅ Server logs show:
```
[DEBUG] HandlePlayerReady START
[DEBUG] Set Player1Ready=true
[DEBUG] Set Player2Ready=true
[DEBUG] game.BothPlayersReady=True
[DEBUG] ✅ GameStart sent to Player1
[DEBUG] ✅ GameStart sent to Player2
```

✅ Client logs show:
```
[DEBUG][GameVM] RoomId initialized = '<UUID>'
[DEBUG] Set _currentRoomId = '<UUID>'
[DEBUG] SendPlayerReadyAsync roomId='<UUID>'
[DEBUG] OnGameStateChanged: message.State='YourTurn'
```

---

## 🆘 COMMON ISSUES

| Problem | Solution |
|---------|----------|
| "Connection failed" | Check server running: `lsof -i :5555` |
| "roomId is empty" | Check GameViewModel logs for initialization |
| "Game doesn't start" | Check server logs for "BothPlayersReady=True" |
| "UI shows wrong state" | Check OnGameStateChanged logs in client |

---

## 📁 Files Modified

- ✅ `client/Services/NetworkService.cs`
- ✅ `client/ViewModels/GameViewModel.cs`
- ✅ `client/MainWindow.axaml.cs`
- ✅ `client/ViewModels/LobbyViewModel.cs`
- ✅ `battle_of_sea/Network/WebSocketListener.cs`

---

## 📚 Full Documentation

See `TEST_GUIDE.md` for complete details and troubleshooting.

---

**Ready to test?** Just run: `./run_ui_test.sh`
