using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using battle_of_sea.Game;
using battle_of_sea.Protocol;

namespace battle_of_sea.Network;

public class WebSocketListener
{
    private readonly int _port;
    private HttpListener? _httpListener;
    private List<WebSocketConnection> _connections = new();

    public WebSocketListener(int port)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _httpListener.Prefixes.Add($"http://localhost:{_port}/");
        _httpListener.Start();

        Console.WriteLine($"WebSocket server started on port {_port}");

        while (true)
        {
            HttpListenerContext context;
            try
            {
                context = await _httpListener.GetContextAsync();
                Console.WriteLine($"Request received: {context.Request.HttpMethod} {context.Request.RawUrl}");
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (context.Request.IsWebSocketRequest)
            {
                Console.WriteLine("✅ WebSocket request detected");
                ProcessWebSocketRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        HttpListenerWebSocketContext webSocketContext;
        try
        {
            webSocketContext = await context.AcceptWebSocketAsync(null);
            using (var webSocket = webSocketContext.WebSocket)
            {
                var connection = new WebSocketConnection(webSocket);
                _connections.Add(connection);
                GameServer.Instance.AddConnection(connection);
                Console.WriteLine($"Client connected. Total connections: {_connections.Count}");
                
                await connection.HandleAsync();
                
                _connections.Remove(connection);
                GameServer.Instance.RemoveConnection(connection);
                Console.WriteLine($"Client disconnected. Total connections: {_connections.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    public void Stop()
    {
        _httpListener?.Stop();
        _httpListener?.Close();
    }
}

public class WebSocketConnection
{
    private readonly WebSocket _webSocket;
    private Player? _player;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

    public WebSocketConnection(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public async Task HandleAsync()
    {
        try
        {
            var buffer = new byte[1024 * 64]; // Увеличили буфер
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            while (_webSocket.State == WebSocketState.Open)
            {
                Console.WriteLine($"[WebSocketConnection] Waiting for message... WebSocket State: {_webSocket.State}");
                
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    Console.WriteLine($"[WebSocketConnection] Received {result.Count} bytes, Type: {result.MessageType}, EndOfMessage: {result.EndOfMessage}, State: {_webSocket.State}");

                    if (result.Count == 0)
                    {
                        Console.WriteLine($"[WebSocketConnection] Received 0 bytes - client closed connection gracefully");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"[WebSocketConnection] Close message received");
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None
                        );
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text && result.EndOfMessage)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received: {json}");

                        JsonElement root;
                        try
                        {
                            root = JsonSerializer.Deserialize<JsonElement>(json, options);
                        }
                        catch
                        {
                            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Invalid JSON" } });
                            continue;
                        }

                        if (!root.TryGetProperty("type", out var typeElem) || string.IsNullOrEmpty(typeElem.GetString()))
                        {
                            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Missing Type" } });
                            continue;
                        }

                        var messageType = typeElem.GetString()!;
                        
                        // Пытаемся получить payload, если его нет - используем root как payload
                        JsonElement payload;
                        if (!root.TryGetProperty("payload", out payload))
                        {
                            // Если нет поля payload, то весь root - это и есть payload
                            payload = root;
                        }

                        await HandleMessageAsync(messageType, payload);
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"[WebSocketConnection] WebSocket error: {ex.GetType().Name}: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"[WebSocketConnection] InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    Console.WriteLine($"[WebSocketConnection] StackTrace: {ex.StackTrace}");
                    Console.WriteLine($"[WebSocketConnection] WebSocket State: {_webSocket.State}");
                    break;
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine($"[WebSocketConnection] Operation cancelled: {ex.Message}");
                    Console.WriteLine($"[WebSocketConnection] StackTrace: {ex.StackTrace}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            // Удаляем соединение со списка
            GameServer.Instance.RemoveConnection(this);

            if (_player != null)
            {
                Console.WriteLine($"Player disconnected: {_player.Name}");
                // Удаляем игрока из GameManager
                GameServer.Instance.GameManager.RemovePlayer(_player.Id);
            }
            _webSocket?.Dispose();
        }
    }

    private async Task HandleMessageAsync(string messageType, JsonElement payload)
    {
        Console.WriteLine($"[HandleMessageAsync] Processing message type: {messageType}");
        
        switch (messageType.ToLower())
        {
            case "connect":
                await HandleConnect(payload);
                break;

            case "ping":
                await SendAsync(new ServerMessage { Type = "pong", Payload = new { } });
                break;

            case "shoot":
                await HandleShoot(payload);
                break;

            case "shipplacement":
                await HandleShipPlacement(payload);
                break;

            case "reconnect":
                await HandleReconnect(payload);
                break;

            case "getrooms":
                await HandleGetRooms();
                break;

            case "createroom":
                await HandleCreateRoom(payload);
                break;

            case "joinroom":
                await HandleJoinRoom(payload);
                break;

            case "leaveroom":
                await HandleLeaveRoom(payload);
                break;

            case "chatmessage":
                // Чат игнорируем пока
                break;

            default:
                await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Unknown command" } });
                break;
        }
    }

    private async Task HandleConnect(JsonElement payload)
    {
        try
        {
            Console.WriteLine($"[HandleConnect] Payload ValueKind: {payload.ValueKind}");
            
            if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
            {
                throw new InvalidOperationException("Payload is null or undefined");
            }
            
            Console.WriteLine($"[HandleConnect] Starting...");
            var playerName = "Unknown";
            var userId = Guid.NewGuid().ToString();
            
            if (payload.TryGetProperty("displayName", out var displayNameElem))
            {
                playerName = displayNameElem.GetString() ?? "Unknown";
            }
            
            if (payload.TryGetProperty("userId", out var userIdElem))
            {
                userId = userIdElem.GetString() ?? Guid.NewGuid().ToString();
            }

            Console.WriteLine($"[HandleConnect] Creating player: {playerName} ({userId})");
            
            _player = new Player
            {
                Id = userId,
                Name = playerName,
                Connection = this
            };

            Console.WriteLine($"[HandleConnect] Adding player to GameManager...");
            GameServer.Instance.GameManager.AddPlayer(_player);

            Console.WriteLine($"[HandleConnect] Sending connected message...");
            await SendAsync(new ServerMessage
            {
                Type = "connected",
                Payload = new { playerId = _player.Id, displayName = _player.Name }
            });

            // Отправляем список комнат при подключении
            Console.WriteLine($"[HandleConnect] Sending rooms list...");
            await BroadcastRoomsList();

            Console.WriteLine($"✅ Player connected: {playerName} ({_player.Id})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleConnect] ❌ EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[HandleConnect] Stack trace: {ex.StackTrace}");
            try
            {
                await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
            }
            catch (Exception sendEx)
            {
                Console.WriteLine($"[HandleConnect] Failed to send error message: {sendEx.Message}");
            }
        }
    }

    private async Task HandleShoot(JsonElement payload)
    {
        try
        {
            if (_player == null)
            {
                Console.WriteLine($"[HandleShoot] ❌ Player is null");
                await SendAsync(new ServerMessage
                {
                    Type = "error",
                    Payload = new { message = "Not connected" }
                });
                return;
            }

            var game = GameServer.Instance.GameManager.FindGameByPlayerId(_player.Id);
            if (game == null)
            {
                Console.WriteLine($"[HandleShoot] ❌ No game found for player {_player.Name}");
                await SendAsync(new ServerMessage
                {
                    Type = "error",
                    Payload = new { message = "Not in a game" }
                });
                return;
            }

            var row = payload.GetProperty("row").GetInt32();
            var col = payload.GetProperty("col").GetInt32();
            
            Console.WriteLine($"[HandleShoot] 🎯 Player {_player.Name} shooting at ({row},{col})");

            await game.ProcessShotAsync(_player, row, col);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleShoot] ❌ Error: {ex.Message}");
            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
        }
    }

    private async Task HandleShipPlacement(JsonElement payload)
    {
        try
        {
            if (_player == null)
            {
                await SendAsync(new ServerMessage
                {
                    Type = "error",
                    Payload = new { message = "Not connected" }
                });
                return;
            }

            var roomId = payload.GetProperty("roomId").GetString();
            
            // Найти комнату где находится игрок
            var room = GameServer.Instance.GameManager.FindRoomById(roomId);
            Console.WriteLine($"[HandleShipPlacement] Player {_player.Name} (ID: {_player.Id}) sending ships for room {roomId}");
            Console.WriteLine($"[HandleShipPlacement] Room found: {room != null}, Room players: {room?.Players.Count ?? 0}");
            
            if (room != null)
            {
                Console.WriteLine($"[HandleShipPlacement] Players in room: {string.Join(", ", room.Players.Select(p => $"{p.Name}({p.Id})"))}");
                Console.WriteLine($"[HandleShipPlacement] Is player in room? {room.Players.Contains(_player)}");
            }
            
            if (room == null)
            {
                await SendAsync(new ServerMessage
                {
                    Type = "error",
                    Payload = new { message = "Room not found" }
                });
                return;
            }

            if (!room.Players.Contains(_player))
            {
                // Если в комнате есть место, автоматически присоединяем игрока
                if (room.Players.Count < room.MaxPlayers)
                {
                    Console.WriteLine($"[HandleShipPlacement] Player not in room but slot available - auto-joining player {_player.Id} to room {room.Id}");
                    GameServer.Instance.GameManager.JoinRoom(_player, room);

                    // Отправляем подтверждение присоединения клиенту
                    await SendAsync(new ServerMessage
                    {
                        Type = "JoinRoom",
                        Payload = new { success = true, roomId = room.Id }
                    });

                    Console.WriteLine($"[HandleShipPlacement] Auto-joined player {_player.Name} to room {room.Name}");
                }
                else
                {
                    await SendAsync(new ServerMessage
                    {
                        Type = "error",
                        Payload = new { message = "Not in a room" }
                    });
                    return;
                }
            }

            Console.WriteLine($"[HandleShipPlacement] Processing ships for player {_player.Name} in room {room.Name}");

            // Получить корабли из сообщения
            if (payload.TryGetProperty("ships", out var shipsElement))
            {
                var ships = shipsElement.EnumerateArray().ToList();
                Console.WriteLine($"[HandleShipPlacement] Received {ships.Count} ships");
                
                // Загрузить корабли на доску игрока
                foreach (var shipJson in ships)
                {
                    var row = shipJson.GetProperty("Row").GetInt32();
                    var col = shipJson.GetProperty("Col").GetInt32();
                    var size = shipJson.GetProperty("Size").GetInt32();
                    var isHorizontal = shipJson.GetProperty("IsHorizontal").GetBoolean();
                    
                    // Здесь должна быть логика размещения корабля на доске
                    // Пока просто отмечаем что игрок разместил корабли
                }
            }

            // Отметить что игрок готов
            _player.IsReady = true;
            Console.WriteLine($"[HandleShipPlacement] Player {_player.Name} marked as ready");

            // Отправить подтверждение
            await SendAsync(new ServerMessage
            {
                Type = "shipPlacementResult",
                Payload = new { success = true, message = "Ships placed successfully" }
            });

            // Проверить если оба игрока готовы
            if (room.Players.Count == 2 && room.Players.All(p => p.IsReady))
            {
                Console.WriteLine($"[HandleShipPlacement] Both players ready! Checking game state...");

                // Не создаём новую сессию, если игра уже запущена
                var existingGamePlayer1 = GameServer.Instance.GameManager.FindGameByPlayerId(room.Players[0].Id);
                var existingGamePlayer2 = GameServer.Instance.GameManager.FindGameByPlayerId(room.Players[1].Id);
                if (room.IsGameStarted || existingGamePlayer1 != null || existingGamePlayer2 != null)
                {
                    Console.WriteLine($"[HandleShipPlacement] Game already started for this room - skipping creation");
                }
                else
                {
                    Console.WriteLine($"[HandleShipPlacement] Starting new GameSession for room {room.Name}...");

                    // Создаем GameSession
                    var game = new GameSession(room.Players[0], room.Players[1]);
                    game.GameFinished += GameServer.Instance.GameManager.FinishGame;
                    GameServer.Instance.GameManager.ActiveGames.Add(game);
                    room.IsGameStarted = true;

                    Console.WriteLine($"[HandleShipPlacement] Game started in room {room.Name}: {room.Players[0].Name} vs {room.Players[1].Name}");

                    // Оповещаем обоих игроков что игра началась
                    foreach (var player in room.Players)
                    {
                        if (player.Connection is WebSocketConnection conn)
                        {
                            await conn.SendAsync(new ServerMessage
                            {
                                Type = "gameStarted",
                                Payload = new { message = "Game started!" }
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleShipPlacement] ERROR: {ex.Message}");
            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
        }
    }

    private async Task HandleReconnect(JsonElement payload)
    {
        try
        {
            var playerId = payload.GetProperty("playerId").GetString();

            var player = GameServer.Instance.GameManager.FindPlayerById(playerId);
            if (player == null)
            {
                await SendAsync(new ServerMessage
                {
                    Type = "error",
                    Payload = new { message = "Player not found" }
                });
                return;
            }

            player.Connection = this;
            _player = player;

            var opponentName = GameServer.Instance.GameManager
                .FindGameByPlayerId(player.Id)
                ?.GetOpponentPlayer()?.Name ?? "Unknown";

            await SendAsync(new ServerMessage
            {
                Type = "reconnected",
                Payload = new
                {
                    playerId = player.Id,
                    opponent = opponentName
                }
            });

            Console.WriteLine($"Player reconnected: {player.Name}");
        }
        catch (Exception ex)
        {
            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
        }
    }

    private async Task HandleGetRooms()
    {
        try
        {
            var rooms = GameServer.Instance.GameManager.GetRooms()
                .Select(r => new { r.Id, r.Name, r.MaxPlayers, Players = r.Players.Count, r.IsGameStarted })
                .ToList();
            
            await SendAsync(new ServerMessage
            {
                Type = "RoomsList",
                Payload = new { rooms = rooms }
            });
        }
        catch (Exception ex)
        {
            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
        }
    }

    private async Task HandleCreateRoom(JsonElement payload)
    {
        try
        {
            var roomName = payload.GetProperty("roomName").GetString() ?? "Room";
            var maxPlayers = payload.TryGetProperty("maxPlayers", out var mp) ? mp.GetInt32() : 2;

            Console.WriteLine($"[CreateRoom] Creating room: {roomName}, max players: {maxPlayers}");
            var room = GameServer.Instance.GameManager.CreateRoom(roomName, maxPlayers);
            Console.WriteLine($"[CreateRoom] Room created with ID: {room.Id}");

            // 🔥 АВТОМАТИЧЕСКИ добавляем создателя в комнату
            if (_player != null)
            {
                Console.WriteLine($"[CreateRoom] Adding creator to room...");
                GameServer.Instance.GameManager.JoinRoom(_player, room);
                Console.WriteLine($"[CreateRoom] Creator added to room");
            }

            await SendAsync(new ServerMessage
            {
                Type = "RoomCreated",
                Payload = new { room = new { room.Id, room.Name, room.MaxPlayers, Players = room.Players.Count } }
            });

            Console.WriteLine($"[CreateRoom] Sent RoomCreated message to creator");

            // Отправляем обновленный список комнат всем клиентам
            Console.WriteLine($"[CreateRoom] Broadcasting rooms list to all clients");
            await BroadcastRoomsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateRoom] ERROR: {ex.Message}");
            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
        }
    }

    private async Task BroadcastRoomsList()
    {
        try
        {
            var rooms = GameServer.Instance.GameManager.GetRooms()
                .Select(r => new { r.Id, r.Name, r.MaxPlayers, Players = r.Players.Count })
                .ToList();

            Console.WriteLine($"[BroadcastRoomsList] Found {rooms.Count} rooms");

            var roomsMessage = new ServerMessage
            {
                Type = "RoomsList",
                Payload = new { rooms = rooms }
            };

            // Отправляем всем подключенным клиентам
            var allConnections = GameServer.Instance.GetAllConnections();
            Console.WriteLine($"[BroadcastRoomsList] Sending to {allConnections.Count} connected clients");
            
            foreach (var connection in allConnections)
            {
                await connection.SendAsync(roomsMessage);
            }
            
            Console.WriteLine($"[BroadcastRoomsList] ✅ Broadcast complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BroadcastRoomsList] ❌ ERROR: {ex.Message}");
        }
    }

    private async Task HandleJoinRoom(JsonElement payload)
    {
        try
        {
            Console.WriteLine($"[HandleJoinRoom] Starting...");
            
            if (_player == null)
            {
                Console.WriteLine($"[HandleJoinRoom] ❌ Player is null");
                await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Not connected" } });
                return;
            }

            var roomId = payload.GetProperty("roomId").GetString();
            Console.WriteLine($"[HandleJoinRoom] Looking for room: {roomId}");
            
            var room = GameServer.Instance.GameManager.FindRoomById(roomId);

            if (room == null)
            {
                Console.WriteLine($"[HandleJoinRoom] ❌ Room not found: {roomId}");
                await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Room not found" } });
                return;
            }

            Console.WriteLine($"[HandleJoinRoom] Found room: {room.Name}");
            Console.WriteLine($"[HandleJoinRoom] Room players BEFORE join: {string.Join(", ", room.Players.Select(p => $"{p.Name}({p.Id})"))}");
            GameServer.Instance.GameManager.JoinRoom(_player, room);
            Console.WriteLine($"[HandleJoinRoom] Room players AFTER join: {string.Join(", ", room.Players.Select(p => $"{p.Name}({p.Id})"))}");

            await SendAsync(new ServerMessage
            {
                Type = "JoinRoom",
                Payload = new { success = true, roomId = room.Id }
            });

            Console.WriteLine($"✅ Player {_player.Name} joined room {room.Name}");
            
            // Если игра начата, отправляем сообщение что игра началась
            if (room.IsGameStarted)
            {
                Console.WriteLine($"[HandleJoinRoom] Game started in room {room.Name}");
                // Отправляем всем игрокам в комнате что игра началась
                await BroadcastGameStarted(room);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleJoinRoom] ❌ Exception: {ex.Message}");
            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
        }
    }

    private async Task HandleLeaveRoom(JsonElement payload)
    {
        try
        {
            if (_player == null)
            {
                await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Not connected" } });
                return;
            }

            var game = GameServer.Instance.GameManager.FindGameByPlayerId(_player.Id);
            if (game != null)
            {
                GameServer.Instance.GameManager.RemoveGame(game);
            }

            await SendAsync(new ServerMessage
            {
                Type = "LeftRoom",
                Payload = new { success = true }
            });

            Console.WriteLine($"Player {_player.Name} left room");
        }
        catch (Exception ex)
        {
            await SendAsync(new ServerMessage { Type = "error", Payload = new { message = ex.Message } });
        }
    }

    private async Task BroadcastGameStarted(Room room)
    {
        try
        {
            var connections = GameServer.Instance.GetAllConnections();
            Console.WriteLine($"[BroadcastGameStarted] Broadcasting to {connections.Count} connections");
            
            foreach (var connection in connections)
            {
                // Отправляем сообщение только игрокам в этой комнате
                if (room.Players.Any(p => p.Id == connection._player?.Id))
                {
                    await connection.SendAsync(new ServerMessage
                    {
                        Type = "GameStarted",
                        Payload = new { roomId = room.Id, message = "Game has started!" }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BroadcastGameStarted] Error: {ex.Message}");
        }
    }

    public async Task SendAsync(ServerMessage message)
    {
        try
        {
            await _sendSemaphore.WaitAsync();
            
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    var json = JsonSerializer.Serialize(message);
                        Console.WriteLine($"[SendAsync] Sending {message.Type}: {json.Substring(0, Math.Min(100, json.Length))}...");
                    var buffer = Encoding.UTF8.GetBytes(json);

                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                    Console.WriteLine($"[SendAsync] ✅ Message sent");
                }
                else
                {
                    Console.WriteLine($"[SendAsync] ❌ WebSocket not open, state: {_webSocket.State}");
                }
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendAsync] ❌ Error sending message: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[SendAsync] InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                Console.WriteLine($"[SendAsync] StackTrace: {ex.StackTrace}");
            }
    }
}
