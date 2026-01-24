#!/usr/bin/env python3
import asyncio
import json
import websockets
import sys

class TestClient:
    def __init__(self, name, url="ws://localhost:5555"):
        self.name = name
        self.url = url
        self.ws = None
        self.rooms = []
        
    async def connect(self):
        try:
            self.ws = await websockets.connect(self.url)
            print(f"[{self.name}] ✅ Connected to server")
            
            # Send Connect message
            connect_msg = {
                "type": "Connect",
                "userId": self.name,
                "displayName": self.name
            }
            await self.ws.send(json.dumps(connect_msg))
            print(f"[{self.name}] Sent Connect message")
            
            # Listen for messages
            asyncio.create_task(self.listen())
            
        except Exception as e:
            print(f"[{self.name}] ❌ Connection error: {e}")
            
    async def listen(self):
        try:
            async for message in self.ws:
                await self.handle_message(message)
        except websockets.exceptions.ConnectionClosed:
            print(f"[{self.name}] Connection closed")
            
    async def handle_message(self, json_str):
        try:
            data = json.loads(json_str)
            msg_type = data.get("Type", "Unknown")
            
            if msg_type == "RoomsList":
                payload = data.get("Payload", {})
                rooms = payload.get("rooms", [])
                self.rooms = rooms
                print(f"[{self.name}] 📍 Received RoomsList with {len(rooms)} room(s)")
                for room in rooms:
                    print(f"[{self.name}]   - {room.get('Name')} ({room.get('Players')}/{room.get('MaxPlayers')})")
            elif msg_type == "RoomCreated":
                print(f"[{self.name}] ✅ Room created")
            elif msg_type == "connected":
                print(f"[{self.name}] ✅ Connection confirmed")
            else:
                print(f"[{self.name}] Received {msg_type}")
        except Exception as e:
            print(f"[{self.name}] Error handling message: {e}")
    
    async def create_room(self, room_name):
        msg = {
            "type": "CreateRoom",
            "roomName": room_name,
            "maxPlayers": 2
        }
        await self.ws.send(json.dumps(msg))
        print(f"[{self.name}] Sent CreateRoom: {room_name}")
    
    async def get_rooms(self):
        msg = {"type": "GetRooms"}
        await self.ws.send(json.dumps(msg))
        print(f"[{self.name}] Requested rooms list")
    
    async def join_room(self, room_id):
        msg = {
            "type": "JoinRoom",
            "roomId": room_id,
            "userId": self.name
        }
        await self.ws.send(json.dumps(msg))
        print(f"[{self.name}] Sent JoinRoom request")

async def main():
    print("=== Room Broadcast & Join Test ===\n")
    
    # Create two clients
    client1 = TestClient("Client1")
    client2 = TestClient("Client2")
    
    # Connect both
    await client1.connect()
    await client2.connect()
    
    # Wait for initial connection and room list
    await asyncio.sleep(1)
    
    # TEST 1: Create room
    print("\n[Test 1] Client1 creating room 'Test Room'...")
    await client1.create_room("Test Room")
    
    # Wait for broadcast
    await asyncio.sleep(2)
    
    print(f"[Test 1] Client2 received {len(client2.rooms)} room(s)")
    
    if len(client2.rooms) > 0:
        print("✅ TEST 1 PASSED: Room appeared in Client2!")
        room = client2.rooms[0]
        print(f"   Room: {room.get('Name')} ({room.get('Players')}/{room.get('MaxPlayers')})")
        
        # TEST 2: Join room
        print("\n[Test 2] Client2 joining room...")
        room_id = room.get('Id')
        await client2.join_room(room_id)
        
        # Wait for join broadcast
        await asyncio.sleep(2)
        
        print(f"[Test 2] Client1 room count: {len(client1.rooms)}")
        if len(client1.rooms) > 0:
            updated_room = client1.rooms[0]
            players_count = updated_room.get('Players')
            print(f"   Updated room: {updated_room.get('Name')} ({players_count}/{updated_room.get('MaxPlayers')})")
            if players_count == 2:
                print("✅ TEST 2 PASSED: Room now shows 2 players on Client1!")
            else:
                print(f"❌ TEST 2 FAILED: Expected 2 players, got {players_count}")
        else:
            print("❌ TEST 2 FAILED: No rooms on Client1")
    else:
        print("❌ TEST 1 FAILED: Room did NOT appear in Client2!")

if __name__ == "__main__":
    asyncio.run(main())
