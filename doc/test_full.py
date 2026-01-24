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
                pass
        except Exception as e:
            pass
    
    async def create_room(self, room_name):
        msg = {
            "type": "CreateRoom",
            "roomName": room_name,
            "maxPlayers": 2
        }
        await self.ws.send(json.dumps(msg))
        print(f"[{self.name}] Sent CreateRoom: {room_name}")
    
    async def join_room(self, room_id):
        msg = {
            "type": "JoinRoom",
            "roomId": room_id,
            "userId": self.name
        }
        await self.ws.send(json.dumps(msg))
        print(f"[{self.name}] Sent JoinRoom request")

async def main():
    print("=== Full Room Synchronization Test (3 Clients) ===\n")
    
    # Create three clients
    client1 = TestClient("Client1")
    client2 = TestClient("Client2")
    client3 = TestClient("Client3")
    
    # Connect all
    await client1.connect()
    await client2.connect()
    await client3.connect()
    
    # Wait for initial connection
    await asyncio.sleep(1)
    
    # TEST 1: Create room in Client1
    print("\n=== TEST 1: Create Room ===")
    print("[Test] Client1 creating room 'Battle Room'...")
    await client1.create_room("Battle Room")
    await asyncio.sleep(1)
    
    success1 = len(client1.rooms) > 0 and len(client2.rooms) > 0 and len(client3.rooms) > 0
    if success1:
        print(f"✅ All 3 clients see the room!")
        print(f"   Client1: {len(client1.rooms)} room(s)")
        print(f"   Client2: {len(client2.rooms)} room(s)")
        print(f"   Client3: {len(client3.rooms)} room(s)")
    else:
        print(f"❌ Not all clients see the room!")
        return
    
    # TEST 2: Join room from Client2
    print("\n=== TEST 2: Join Room ===")
    # Find "Battle Room" (the new one created by Client1)
    battle_room = None
    for room in client2.rooms:
        if room.get('Name') == 'Battle Room':
            battle_room = room
            break
    
    if not battle_room:
        print(f"❌ Could not find Battle Room!")
        return
    
    print(f"[Test] Client2 joining 'Battle Room'...")
    await client2.join_room(battle_room.get('Id'))
    await asyncio.sleep(1)
    
    # Find Battle Room in all clients
    battle_room1 = None
    battle_room2 = None
    battle_room3 = None
    
    for room in client1.rooms:
        if room.get('Name') == 'Battle Room':
            battle_room1 = room
            break
    for room in client2.rooms:
        if room.get('Name') == 'Battle Room':
            battle_room2 = room
            break
    for room in client3.rooms:
        if room.get('Name') == 'Battle Room':
            battle_room3 = room
            break
    
    if battle_room1 and battle_room2 and battle_room3:
        p1 = battle_room1.get('Players')
        p2 = battle_room2.get('Players')
        p3 = battle_room3.get('Players')
        
        if p1 == 2 and p2 == 2 and p3 == 2:
            print(f"✅ All 3 clients see Battle Room with 2 players!")
            print(f"   Client1: {p1}/2 players")
            print(f"   Client2: {p2}/2 players")
            print(f"   Client3: {p3}/2 players")
        else:
            print(f"❌ Player count mismatch!")
            print(f"   Client1: {p1}/2, Client2: {p2}/2, Client3: {p3}/2")
    else:
        print(f"❌ Some clients don't have Battle Room!")
    
    # TEST 3: Verify Client3 sees room is full
    print("\n=== TEST 3: Verify Full Room Behavior ===")
    if battle_room3:
        players = battle_room3.get('Players')
        max_players = battle_room3.get('MaxPlayers')
        is_full = players >= max_players
        
        if is_full:
            print(f"✅ Client3 correctly sees Battle Room is FULL ({players}/{max_players})")
        else:
            print(f"❌ Client3 doesn't see Battle Room as full ({players}/{max_players})")
    else:
        print(f"❌ Client3 doesn't have Battle Room!")
    
    print("\n=== ✅ All tests completed! ===")

if __name__ == "__main__":
    asyncio.run(main())
