# 💻 Примеры кода для тестирования

## Python 🐍

### Полный пример (2 игрока)

```python
#!/usr/bin/env python3
import socket
import json
import time

class GameClient:
    def __init__(self, name, port=5000):
        self.name = name
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect(("localhost", port))
        self.player_id = None
        self.room_id = None
    
    def send_message(self, msg_type, payload=None):
        if payload is None:
            payload = {}
        
        msg = {
            "Type": msg_type,
            "Payload": payload
        }
        
        print(f"[{self.name}] → {msg_type}")
        self.socket.send((json.dumps(msg) + "\n").encode())
        
        # Получить ответ
        response = self.socket.recv(1024).decode()
        data = json.loads(response)
        print(f"[{self.name}] ← {data['Type']}")
        
        return data
    
    def connect(self):
        resp = self.send_message("Connect", {"playerName": self.name})
        self.player_id = resp["Payload"]["playerId"]
        print(f"[{self.name}] Connected! ID: {self.player_id}\n")
    
    def create_room(self, room_name):
        resp = self.send_message("CreateRoom", {"roomName": room_name})
        self.room_id = resp["Payload"]["roomId"]
        print(f"[{self.name}] Room created! ID: {self.room_id}\n")
        
        # Получить трансляцию RoomsList
        rooms_list = self.socket.recv(1024).decode()
        print(f"[{self.name}] ← RoomsList (broadcast)\n")
    
    def join_room(self, room_id):
        resp = self.send_message("JoinRoom", {"roomId": room_id})
        if resp["Payload"]["success"]:
            self.room_id = room_id
            print(f"[{self.name}] Joined room!\n")
            
            # Получить трансляцию RoomsList
            rooms_list = self.socket.recv(1024).decode()
            print(f"[{self.name}] ← RoomsList (broadcast)\n")
        else:
            print(f"[{self.name}] Error: {resp['Payload']['message']}\n")
    
    def ready(self):
        resp = self.send_message("PlayerReady")
        print(f"[{self.name}] Marked as ready!\n")
        return resp
    
    def close(self):
        self.socket.close()

# Пример использования
if __name__ == "__main__":
    # Создать двух клиентов
    player1 = GameClient("Alice")
    player2 = GameClient("Bob")
    
    # Подключить обоих
    player1.connect()
    player2.connect()
    
    # Player1 создаёт комнату
    player1.create_room("Battle Arena")
    
    # Player2 присоединяется к комнате
    time.sleep(0.5)
    player2.join_room(player1.room_id)
    
    # Оба готовы
    player1.ready()
    time.sleep(0.5)
    resp = player2.ready()
    
    # Получить GameStateChanged
    if resp["Type"] == "PlayerReady":
        game_start = player2.socket.recv(1024).decode()
        print(f"[Bob] ← {json.loads(game_start)['Type']}")
        print("🎮 Game Started!")
    
    player1.close()
    player2.close()
```

### Минимальный пример

```python
import socket, json

# Подключиться
sock = socket.socket()
sock.connect(("localhost", 5000))

# Отправить Connect
msg = {"Type": "Connect", "Payload": {"playerName": "TestPlayer"}}
sock.send((json.dumps(msg) + "\n").encode())

# Получить ответ
response = json.loads(sock.recv(1024).decode())
print(response)

sock.close()
```

---

## JavaScript / Node.js 🟨

### Полный пример

```javascript
const net = require('net');

class GameClient {
    constructor(name) {
        this.name = name;
        this.socket = net.createConnection(5000, 'localhost');
        this.playerId = null;
        this.roomId = null;
        
        this.socket.on('data', (data) => {
            const msg = JSON.parse(data.toString());
            console.log(`[${this.name}] ← ${msg.Type}`);
            this.handleMessage(msg);
        });
    }
    
    handleMessage(msg) {
        if (msg.Type === "connected") {
            this.playerId = msg.Payload.playerId;
        } else if (msg.Type === "RoomCreated") {
            this.roomId = msg.Payload.roomId;
        } else if (msg.Type === "JoinRoomResult") {
            this.roomId = msg.Payload.roomId;
        }
    }
    
    send(type, payload = {}) {
        const msg = { Type: type, Payload: payload };
        console.log(`[${this.name}] → ${type}`);
        this.socket.write(JSON.stringify(msg) + "\n");
    }
    
    close() {
        this.socket.end();
    }
}

// Использование
const player1 = new GameClient("Alice");
const player2 = new GameClient("Bob");

setTimeout(() => {
    player1.send("Connect", { playerName: "Alice" });
    player2.send("Connect", { playerName: "Bob" });
}, 100);

setTimeout(() => {
    player1.send("CreateRoom", { roomName: "Battle" });
}, 200);

setTimeout(() => {
    player2.send("JoinRoom", { roomId: player1.roomId });
}, 300);

setTimeout(() => {
    player1.send("PlayerReady");
    player2.send("PlayerReady");
}, 400);

setTimeout(() => {
    player1.close();
    player2.close();
}, 1000);
```

### Браузер (WebSocket через Node.js прокси)

```javascript
// Подключиться к серверу
const ws = new WebSocket('ws://localhost:8080');

ws.onopen = () => {
    // Отправить Connect
    ws.send(JSON.stringify({
        Type: "Connect",
        Payload: { playerName: "BrowserPlayer" }
    }));
};

ws.onmessage = (event) => {
    const msg = JSON.parse(event.data);
    console.log("Server:", msg.Type, msg.Payload);
    
    if (msg.Type === "RoomsList") {
        // Обновить UI со списком комнат
        updateRoomsList(msg.Payload.rooms);
    } else if (msg.Type === "GameStateChanged") {
        // Начать игру
        startGame(msg.Payload.players);
    }
};

// Функция для присоединения к комнате
function joinRoom(roomId) {
    ws.send(JSON.stringify({
        Type: "JoinRoom",
        Payload: { roomId: roomId }
    }));
}

// Функция для готовности
function markReady() {
    ws.send(JSON.stringify({
        Type: "PlayerReady",
        Payload: {}
    }));
}
```

---

## C# 🔵

### Полный пример

```csharp
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GameClient
{
    private readonly TcpClient _client;
    private readonly StreamWriter _writer;
    private readonly StreamReader _reader;
    private string _playerId;
    private string _roomId;
    
    public GameClient(string playerName)
    {
        _client = new TcpClient("localhost", 5000);
        var stream = _client.GetStream();
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        _reader = new StreamReader(stream, new UTF8Encoding(false));
        
        ConnectAsync(playerName).Wait();
    }
    
    private async Task ConnectAsync(string playerName)
    {
        await SendAsync("Connect", new { playerName });
        var response = await ReceiveAsync();
        _playerId = response.GetProperty("Payload")
            .GetProperty("playerId").GetString();
        Console.WriteLine($"Connected! ID: {_playerId}");
    }
    
    public async Task CreateRoomAsync(string roomName)
    {
        await SendAsync("CreateRoom", new { roomName });
        var response = await ReceiveAsync();
        _roomId = response.GetProperty("Payload")
            .GetProperty("roomId").GetString();
        Console.WriteLine($"Room created! ID: {_roomId}");
        
        // Получить трансляцию
        await ReceiveAsync();
    }
    
    public async Task JoinRoomAsync(string roomId)
    {
        await SendAsync("JoinRoom", new { roomId });
        var response = await ReceiveAsync();
        var success = response.GetProperty("Payload")
            .GetProperty("success").GetBoolean();
        
        if (success)
        {
            _roomId = roomId;
            Console.WriteLine("Joined room!");
            await ReceiveAsync();
        }
        else
        {
            Console.WriteLine("Failed to join room");
        }
    }
    
    public async Task ReadyAsync()
    {
        await SendAsync("PlayerReady", new { });
        var response = await ReceiveAsync();
        Console.WriteLine("Marked as ready!");
    }
    
    private async Task SendAsync(string type, object payload)
    {
        var msg = new { Type = type, Payload = payload };
        var json = JsonSerializer.Serialize(msg);
        await _writer.WriteLineAsync(json);
    }
    
    private async Task<JsonElement> ReceiveAsync()
    {
        var line = await _reader.ReadLineAsync();
        return JsonSerializer.Deserialize<JsonElement>(line);
    }
    
    public void Close()
    {
        _client.Close();
    }
}

// Использование
public class Program
{
    public static async Task Main()
    {
        var player1 = new GameClient("Alice");
        var player2 = new GameClient("Bob");
        
        await player1.CreateRoomAsync("Arena");
        await Task.Delay(100);
        
        await player2.JoinRoomAsync(player1._roomId);
        await Task.Delay(100);
        
        var ready1 = player1.ReadyAsync();
        var ready2 = player2.ReadyAsync();
        await Task.WhenAll(ready1, ready2);
        
        Console.WriteLine("Game started!");
        
        player1.Close();
        player2.Close();
    }
}
```

---

## curl / bash 📋

### Однострочные команды

```bash
# Подключиться
echo '{"Type":"Connect","Payload":{"playerName":"Player1"}}' | nc localhost 5000

# Создать комнату
echo '{"Type":"CreateRoom","Payload":{"roomName":"Game"}}' | nc localhost 5000

# Присоединиться
echo '{"Type":"JoinRoom","Payload":{"roomId":"xxx"}}' | nc localhost 5000

# Готовность
echo '{"Type":"PlayerReady","Payload":{}}' | nc localhost 5000

# Список комнат
echo '{"Type":"RoomsList","Payload":{}}' | nc localhost 5000
```

### Bash скрипт

```bash
#!/bin/bash

HOST="localhost"
PORT=5000

send_message() {
    local type=$1
    local payload=$2
    echo "{\"Type\":\"$type\",\"Payload\":$payload}" | nc $HOST $PORT
}

# Player 1
echo "=== Player 1 ==="
RESP=$(send_message "Connect" '{"playerName":"Player1"}')
PLAYER1_ID=$(echo $RESP | grep -o '"playerId":"[^"]*"' | cut -d'"' -f4)
echo "Player1 ID: $PLAYER1_ID"

RESP=$(send_message "CreateRoom" '{"roomName":"Game"}')
ROOM_ID=$(echo $RESP | grep -o '"roomId":"[^"]*"' | cut -d'"' -f4)
echo "Room ID: $ROOM_ID"

# Player 2
echo "=== Player 2 ==="
send_message "Connect" '{"playerName":"Player2"}'
send_message "JoinRoom" "{\"roomId\":\"$ROOM_ID\"}"

# Both ready
echo "=== Ready ==="
send_message "PlayerReady" '{}'
send_message "PlayerReady" '{}'

echo "Done!"
```

---

## Java ☕

### Полный пример

```java
import java.io.*;
import java.net.Socket;
import org.json.JSONObject;

public class GameClient {
    private Socket socket;
    private PrintWriter out;
    private BufferedReader in;
    private String playerId;
    
    public GameClient(String host, int port) throws IOException {
        socket = new Socket(host, port);
        out = new PrintWriter(socket.getOutputStream(), true);
        in = new BufferedReader(new InputStreamReader(socket.getInputStream()));
    }
    
    public void connect(String playerName) throws IOException {
        JSONObject msg = new JSONObject();
        msg.put("Type", "Connect");
        JSONObject payload = new JSONObject();
        payload.put("playerName", playerName);
        msg.put("Payload", payload);
        
        out.println(msg.toString());
        
        String response = in.readLine();
        JSONObject resp = new JSONObject(response);
        playerId = resp.getJSONObject("Payload").getString("playerId");
        System.out.println("Connected! ID: " + playerId);
    }
    
    public String createRoom(String roomName) throws IOException {
        JSONObject msg = new JSONObject();
        msg.put("Type", "CreateRoom");
        JSONObject payload = new JSONObject();
        payload.put("roomName", roomName);
        msg.put("Payload", payload);
        
        out.println(msg.toString());
        
        String response = in.readLine();
        JSONObject resp = new JSONObject(response);
        String roomId = resp.getJSONObject("Payload").getString("roomId");
        System.out.println("Room created! ID: " + roomId);
        
        // Read broadcast
        in.readLine();
        return roomId;
    }
    
    public void close() throws IOException {
        socket.close();
    }
    
    public static void main(String[] args) throws IOException {
        GameClient player1 = new GameClient("localhost", 5000);
        GameClient player2 = new GameClient("localhost", 5000);
        
        player1.connect("Alice");
        player2.connect("Bob");
        
        String roomId = player1.createRoom("Battle");
        
        player1.close();
        player2.close();
    }
}
```

---

## PHP 🐘

```php
<?php

class GameClient {
    private $socket;
    private $playerId;
    
    public function __construct($host = "localhost", $port = 5000) {
        $this->socket = fsockopen($host, $port, $errno, $errstr, 30);
        if (!$this->socket) {
            die("Error: $errstr ($errno)\n");
        }
    }
    
    public function send($type, $payload = []) {
        $msg = [
            "Type" => $type,
            "Payload" => $payload
        ];
        
        $json = json_encode($msg);
        fwrite($this->socket, $json . "\n");
        
        $response = fgets($this->socket, 4096);
        return json_decode($response, true);
    }
    
    public function connect($playerName) {
        $resp = $this->send("Connect", ["playerName" => $playerName]);
        $this->playerId = $resp["Payload"]["playerId"];
        echo "Connected! ID: {$this->playerId}\n";
    }
    
    public function createRoom($roomName) {
        $resp = $this->send("CreateRoom", ["roomName" => $roomName]);
        $roomId = $resp["Payload"]["roomId"];
        
        // Read broadcast
        fgets($this->socket, 4096);
        
        return $roomId;
    }
    
    public function close() {
        fclose($this->socket);
    }
}

// Использование
$player1 = new GameClient();
$player1->connect("Alice");
$roomId = $player1->createRoom("Battle");
echo "Room ID: $roomId\n";
$player1->close();
?>
```

---

## Go 🐹

```go
package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"net"
)

type Message struct {
	Type    string      `json:"Type"`
	Payload interface{} `json:"Payload"`
}

type GameClient struct {
	conn     net.Conn
	playerId string
}

func NewGameClient(host string, port string) (*GameClient, error) {
	conn, err := net.Dial("tcp", host+":"+port)
	if err != nil {
		return nil, err
	}
	return &GameClient{conn: conn}, nil
}

func (c *GameClient) Send(msgType string, payload interface{}) (map[string]interface{}, error) {
	msg := Message{Type: msgType, Payload: payload}
	data, _ := json.Marshal(msg)
	fmt.Fprintf(c.conn, "%s\n", string(data))

	scanner := bufio.NewScanner(c.conn)
	scanner.Scan()
	
	var resp map[string]interface{}
	json.Unmarshal(scanner.Bytes(), &resp)
	return resp, nil
}

func (c *GameClient) Connect(playerName string) error {
	resp, _ := c.Send("Connect", map[string]string{"playerName": playerName})
	c.playerId = resp["Payload"].(map[string]interface{})["playerId"].(string)
	fmt.Printf("Connected! ID: %s\n", c.playerId)
	return nil
}

func (c *GameClient) Close() {
	c.conn.Close()
}

func main() {
	client, _ := NewGameClient("localhost", "5000")
	client.Connect("Alice")
	client.Close()
}
```

---

**Все примеры готовы! Выбери свой язык и начни тестировать! 🚀**
