#!/usr/bin/env dotnet-script

#r "nuget: WebSocketSharp, 1.0.3-rc11"

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

class RoomBroadcastTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Room Broadcast Test ===\n");
        
        var client1 = new WebSocketClient("Client1");
        var client2 = new WebSocketClient("Client2");
        
        Task.Run(() => client1.Connect());
        Task.Run(() => client2.Connect());
        
        Thread.Sleep(1000);
        
        Console.WriteLine("\n[Test] Client1 creating room 'Test Room'...");
        client1.SendMessage(new { type = "CreateRoom", roomName = "Test Room", maxPlayers = 2 });
        
        Thread.Sleep(2000);
        
        Console.WriteLine("\n[Test] Checking if Client2 received the room...");
        Console.WriteLine($"[Test] Client2 rooms count: {client2.RoomsCount}");
        
        if (client2.RoomsCount > 0)
        {
            Console.WriteLine("✅ SUCCESS: Room appeared in Client2!");
        }
        else
        {
            Console.WriteLine("❌ FAIL: Room did NOT appear in Client2!");
        }
        
        Thread.Sleep(100);
    }
}

class WebSocketClient
{
    private WebSocket _ws;
    private string _clientName;
    private List<string> _receivedRooms = new();
    
    public int RoomsCount => _receivedRooms.Count;
    
    public WebSocketClient(string name)
    {
        _clientName = name;
    }
    
    public void Connect()
    {
        _ws = new WebSocket("ws://localhost:5555");
        
        _ws.OnOpen += () =>
        {
            Console.WriteLine($"[{_clientName}] ✅ Connected");
            SendMessage(new { type = "Connect", userId = _clientName, displayName = _clientName });
        };
        
        _ws.OnMessage += (msg) =>
        {
            try
            {
                using (var doc = JsonDocument.Parse(msg))
                {
                    var type = doc.RootElement.GetProperty("Type").GetString();
                    Console.WriteLine($"[{_clientName}] Received: {type}");
                    
                    if (type == "RoomsList")
                    {
                        var rooms = doc.RootElement.GetProperty("Payload").GetProperty("rooms");
                        _receivedRooms.Clear();
                        foreach (var room in rooms.EnumerateArray())
                        {
                            var roomName = room.GetProperty("Name").GetString();
                            _receivedRooms.Add(roomName);
                            Console.WriteLine($"[{_clientName}]   - Room: {roomName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_clientName}] Error parsing message: {ex.Message}");
            }
        };
        
        _ws.OnError += (err) => Console.WriteLine($"[{_clientName}] ❌ Error: {err}");
        _ws.OnClose += () => Console.WriteLine($"[{_clientName}] Disconnected");
        
        _ws.Connect();
    }
    
    public void SendMessage(object message)
    {
        _ws.Send(JsonSerializer.Serialize(message));
    }
}
