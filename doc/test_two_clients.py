#!/usr/bin/env python3
"""
Test script that simulates two WebSocket clients to verify the ready/game start flow.
- Client 1: Creates a room
- Client 2: Joins the room
- Both: Send PlayerReady
- Server: Should send GameStart to both
"""
import asyncio
import json
import websockets
from datetime import datetime

class WebSocketClient:
    def __init__(self, client_id):
        self.client_id = client_id
        self.ws = None
        self.user_id = f"test_user_{client_id}"
        self.room_id = None
        self.messages_received = []
        self.messages_sent = []
        
    async def connect(self, uri):
        """Connect to WebSocket server"""
        try:
            self.ws = await websockets.connect(uri)
            self.log(f"✅ Connected to server")
            return True
        except Exception as e:
            self.log(f"❌ Connection failed: {e}")
            return False
    
    async def send_message(self, msg_dict):
        """Send JSON message and log it"""
        try:
            json_msg = json.dumps(msg_dict)
            self.log(f"📤 SEND: {json_msg}")
            self.messages_sent.append((datetime.now(), msg_dict))
            await self.ws.send(json_msg)
            return True
        except Exception as e:
            self.log(f"❌ Send failed: {e}")
            return False
    
    async def connect_to_server(self):
        """Send Connect message"""
        msg = {
            "type": "Connect",
            "userId": self.user_id,
            "displayName": f"TestPlayer{self.client_id}"
        }
        await self.send_message(msg)
    
    async def create_room(self, room_name="TestRoom"):
        """Send CreateRoom message"""
        msg = {
            "type": "CreateRoom",
            "roomName": room_name,
            "maxPlayers": 2,
            "password": None
        }
        await self.send_message(msg)
    
    async def join_room(self, room_id):
        """Send JoinRoom message"""
        msg = {
            "type": "JoinRoom",
            "roomId": room_id,
            "userId": self.user_id,
            "password": None
        }
        await self.send_message(msg)
    
    async def send_player_ready(self):
        """Send PlayerReady message"""
        msg = {
            "type": "PlayerReady",
            "roomId": self.room_id,
            "userId": self.user_id
        }
        await self.send_message(msg)
    
    async def listen(self):
        """Listen for messages from server"""
        try:
            async for message in self.ws:
                self.log(f"📥 RECV: {message}")
                try:
                    msg_obj = json.loads(message)
                    self.messages_received.append((datetime.now(), msg_obj))
                    
                    # Handle specific messages
                    msg_type = msg_obj.get("Type") or msg_obj.get("type")
                    
                    if msg_type == "RoomCreated":
                        payload = msg_obj.get("Payload", {})
                        room = payload.get("room", {})
                        self.room_id = room.get("Id")
                        self.log(f"✅ Room created: {self.room_id}")
                    
                    elif msg_type == "JoinRoom" or msg_type == "joinroom" or msg_type == "JoinRoomResult" or msg_type == "joinroomresult":
                        payload = msg_obj.get("Payload", {})
                        room_id = payload.get("roomId") or payload.get("RoomId")
                        if room_id:
                            self.room_id = room_id
                        success = payload.get("success") or payload.get("Success")
                        self.log(f"✅ JoinRoom result: success={success}, room={self.room_id}")
                    
                    elif msg_type == "GameStateChanged" or msg_type == "gamestatechanged":
                        payload = msg_obj.get("Payload", {})
                        state = payload.get("state")
                        self.log(f"🎮 GameStateChanged: {state}")
                    
                    elif msg_type == "GameStart" or msg_type == "gamestart":
                        self.log(f"🎮 GameStart received!")
                    
                except json.JSONDecodeError as e:
                    self.log(f"❌ JSON decode error: {e}")
        
        except asyncio.CancelledError:
            self.log("Connection closed")
        except Exception as e:
            self.log(f"❌ Listen error: {e}")
    
    def log(self, msg):
        """Print timestamped log message"""
        timestamp = datetime.now().strftime("%H:%M:%S.%f")[:-3]
        print(f"[{timestamp}] [Client{self.client_id}] {msg}")
    
    async def close(self):
        """Close connection"""
        if self.ws:
            await self.ws.close()
            self.log("Disconnected")


async def main():
    print("\n" + "="*80)
    print("TEST: Two WebSocket Clients - Ready/GameStart Flow")
    print("="*80 + "\n")
    
    # Create two clients
    client1 = WebSocketClient(1)
    client2 = WebSocketClient(2)
    
    try:
        # Step 1: Both clients connect
        print("\n[STEP 1] Connecting clients to server...")
        if not await client1.connect("ws://localhost:5555"):
            print("❌ Client 1 connection failed")
            return
        
        if not await client2.connect("ws://localhost:5555"):
            print("❌ Client 2 connection failed")
            return
        
        # Start listening tasks for both clients
        listen_task1 = asyncio.create_task(client1.listen())
        listen_task2 = asyncio.create_task(client2.listen())
        
        await asyncio.sleep(0.5)
        
        # Step 2: Send Connect messages
        print("\n[STEP 2] Sending Connect messages...")
        await client1.connect_to_server()
        await client2.connect_to_server()
        await asyncio.sleep(1)
        
        # Step 3: Client 1 creates a room
        print("\n[STEP 3] Client 1 creates a room...")
        await client1.create_room("TestRoom123")
        await asyncio.sleep(1)
        
        if not client1.room_id:
            print("❌ Client 1 failed to create room")
            # Try to extract room_id from RoomCreated message if available
            for ts, msg in client1.messages_received:
                if msg.get("Type") == "RoomCreated":
                    payload = msg.get("Payload", {})
                    room = payload.get("room", {})
                    client1.room_id = room.get("Id")
                    print(f"✅ Extracted room_id from RoomCreated: {client1.room_id}")
                    break
        
        if client1.room_id:
            # Step 4: Client 2 joins the room
            print(f"\n[STEP 4] Client 2 joins room {client1.room_id}...")
            await client2.join_room(client1.room_id)
            await asyncio.sleep(1)
        
        # Step 5: Both clients send PlayerReady
        print("\n[STEP 5] Both clients send PlayerReady...")
        if client1.room_id:
            await client1.send_player_ready()
            await asyncio.sleep(0.3)
            await client2.send_player_ready()
            await asyncio.sleep(1)
        
        # Wait a bit to see if GameStart arrives
        print("\n[STEP 6] Waiting for GameStart messages...")
        await asyncio.sleep(2)
        
        # Cancel listen tasks
        listen_task1.cancel()
        listen_task2.cancel()
        
        try:
            await listen_task1
            await listen_task2
        except asyncio.CancelledError:
            pass
        
        # Summary
        print("\n" + "="*80)
        print("TEST SUMMARY")
        print("="*80)
        
        print(f"\n[Client 1] Messages sent: {len(client1.messages_sent)}")
        for ts, msg in client1.messages_sent:
            print(f"  - {msg.get('type', msg.get('Type'))}")
        
        print(f"\n[Client 1] Messages received: {len(client1.messages_received)}")
        for ts, msg in client1.messages_received:
            msg_type = msg.get("Type") or msg.get("type")
            print(f"  - {msg_type}")
            # Check for GameStart
            if msg_type in ["GameStart", "gamestart", "GameStateChanged"]:
                print(f"    ✅ GAME STATE CHANGE DETECTED: {msg}")
        
        print(f"\n[Client 2] Messages sent: {len(client2.messages_sent)}")
        for ts, msg in client2.messages_sent:
            print(f"  - {msg.get('type', msg.get('Type'))}")
        
        print(f"\n[Client 2] Messages received: {len(client2.messages_received)}")
        for ts, msg in client2.messages_received:
            msg_type = msg.get("Type") or msg.get("type")
            print(f"  - {msg_type}")
            # Check for GameStart
            if msg_type in ["GameStart", "gamestart", "GameStateChanged"]:
                print(f"    ✅ GAME STATE CHANGE DETECTED: {msg}")
        
        # Final verdict
        print("\n" + "="*80)
        client1_got_gamestate = any(
            m.get("Type") in ["GameStart", "GameStateChanged"] or 
            m.get("type") in ["gamestart", "gamestatechanged"]
            for _, m in client1.messages_received
        )
        client2_got_gamestate = any(
            m.get("Type") in ["GameStart", "GameStateChanged"] or 
            m.get("type") in ["gamestart", "gamestatechanged"]
            for _, m in client2.messages_received
        )
        
        if client1_got_gamestate and client2_got_gamestate:
            print("✅ SUCCESS: Both clients received game state change!")
        else:
            print(f"❌ FAILED: Client1 got gamestate={client1_got_gamestate}, Client2 got gamestate={client2_got_gamestate}")
        
        print("="*80 + "\n")
    
    finally:
        await client1.close()
        await client2.close()


if __name__ == "__main__":
    asyncio.run(main())
