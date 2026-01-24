#!/usr/bin/env python3
import asyncio
import json
import websockets

class TestClient:
    def __init__(self, name, url="ws://localhost:5555"):
        self.name = name
        self.url = url
        self.ws = None
        self.rooms = []
        
    async def connect(self):
        self.ws = await websockets.connect(self.url)
        print(f"[{self.name}] ✅ Connected")
        
        connect_msg = {"type": "Connect", "userId": self.name, "displayName": self.name}
        await self.ws.send(json.dumps(connect_msg))
        
        asyncio.create_task(self.listen())
            
    async def listen(self):
        async for message in self.ws:
            data = json.loads(message)
            if data.get("Type") == "RoomsList":
                rooms = data.get("Payload", {}).get("rooms", [])
                self.rooms = rooms
                for r in rooms:
                    print(f"[{self.name}] Room: {r.get('Name')} ({r.get('Players')}/{r.get('MaxPlayers')})")
    
    async def create_room(self, name):
        await self.ws.send(json.dumps({"type": "CreateRoom", "roomName": name, "maxPlayers": 2}))
        print(f"[{self.name}] Creating: {name}")
    
    async def join_room(self, room_id):
        await self.ws.send(json.dumps({"type": "JoinRoom", "roomId": room_id, "userId": self.name}))
        print(f"[{self.name}] Joining room...")

async def main():
    print("=== Simple Test: 2 Clients, 1 Room ===\n")
    
    c1 = TestClient("Alice")
    c2 = TestClient("Bob")
    
    await c1.connect()
    await c2.connect()
    await asyncio.sleep(1)
    
    # Alice creates room
    print("\n[Test] Alice creates 'Game' room")
    await c1.create_room("Game")
    await asyncio.sleep(1)
    
    print(f"\n[Check] Rooms visible:")
    print(f"  Alice: {len(c1.rooms)} room(s)")
    print(f"  Bob: {len(c2.rooms)} room(s)")
    
    if len(c1.rooms) > 0 and len(c2.rooms) > 0:
        room = c2.rooms[0]
        print(f"\n✅ Both see room: {room.get('Name')} ({room.get('Players')}/{room.get('MaxPlayers')})")
        
        # Bob joins
        print(f"\n[Test] Bob joins room")
        await c2.join_room(room.get('Id'))
        await asyncio.sleep(1)
        
        print(f"\n[Check] After join:")
        if c1.rooms and c2.rooms:
            alice_room = c1.rooms[0]
            bob_room = c2.rooms[0]
            print(f"  Alice sees: {alice_room.get('Name')} ({alice_room.get('Players')}/{alice_room.get('MaxPlayers')})")
            print(f"  Bob sees: {bob_room.get('Name')} ({bob_room.get('Players')}/{bob_room.get('MaxPlayers')})")
            
            if alice_room.get('Players') == 2 and bob_room.get('Players') == 2:
                print("\n✅ SUCCESS: Both see 2/2 players!")
            else:
                print("\n❌ FAIL: Player count is wrong!")

asyncio.run(main())
