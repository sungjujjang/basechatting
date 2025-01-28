import asyncio
import websockets
from api import start_db, get_room, aes_decrypt, BASE_TEXT  # add_message는 사용되지 않으므로 제거

def get_last_message():
    con, cur = start_db()
    cur.execute("SELECT * FROM messages ORDER BY id DESC LIMIT 1")
    last_message = cur.fetchone()
    con.close()
    return last_message

LAST_MESSAGE_ID = get_last_message()[0]  # 초기화
CHECK_TIME = 0.1

async def accept(websocket):
    print("New connection")
    data = await websocket.recv()
    # print(f"Received data: {data}")
    try:
        data = data.split(":")
        roomid = data[0]
        nickname = data[1]
    except:
        await websocket.send("Invalid data")
        await websocket.close()
        return
    else:
        room = get_room(roomid)
        if not room:
            await websocket.send("Room not found")
            await websocket.close()
            return
        # print(f"{room[3]}")
        await websocket.send(f"{room[3]}")
        tollgate = await websocket.recv()
        if tollgate != "go":
            await websocket.send("Invalid key")
            await websocket.close()
            return
        # print(f"New connection: {roomid}")
        # await websocket.send("Connected to the server")
        # await websocket.send(f"You connected to the room {room[1]}")
        global LAST_MESSAGE_ID
        try:
            while True:
                last_message = get_last_message()
                if last_message[0] != LAST_MESSAGE_ID:
                    if int(last_message[1]) == int(roomid):
                        # print(f"New message: {last_message[1]}: {last_message[2]}")
                        await websocket.send(f"{last_message[2]}")
                        LAST_MESSAGE_ID = last_message[0]
                await asyncio.sleep(CHECK_TIME)
        except websockets.exceptions.ConnectionClosed as e:
            print(f"Connection closed: {e}")
            return

async def main():
    server = await websockets.serve(accept, "0.0.0.0", 8765)
    print("chatting server started on ws://0.0.0.0:8765")
    await server.wait_closed()

if __name__ == "__main__":
    asyncio.run(main())