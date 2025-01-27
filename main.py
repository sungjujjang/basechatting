import asyncio
import websockets
from api import start_db  # add_message는 사용되지 않으므로 제거

def get_last_message():
    con, cur = start_db()
    cur.execute("SELECT * FROM messages ORDER BY id DESC LIMIT 1")
    last_message = cur.fetchone()
    con.close()
    print(last_message)
    return last_message

LAST_MESSAGE_ID = get_last_message()[0]  # 초기화

async def accept(websocket):
    data = await websocket.recv()
    try:
        splited_data = data.split(":")
        roomid = splited_data[0]
        key = splited_data[1]
    except:
        await websocket.send("Invalid data")
        websocket.close()
        return
    else:
        
        print(f"New connection: {roomid}, {key}")
        await websocket.send("Connected to the server")
        global LAST_MESSAGE_ID
        while True:
            last_message = get_last_message()
            if last_message[0] != LAST_MESSAGE_ID:
                print(f"New message: {last_message[1]}: {last_message[2]}")
                LAST_MESSAGE_ID = last_message[0]
                await websocket.send(f"{last_message[1]}: {last_message[2]}")

async def main():
    server = await websockets.serve(accept, "0.0.0.0", 8765)
    print("chatting server started on ws://0.0.0.0:8765")
    await server.wait_closed()

if __name__ == "__main__":
    asyncio.run(main())