import fastapi
from fastapi import Request, Response
import uvicorn
from api import *
from pydantic import BaseModel

app = fastapi.FastAPI()

class Message(BaseModel):
    message: str
    roomid: int

@app.post("/addmessage")
def addmessage(data: Message):
    try:
        add_message(data.roomid, f"{data.message}", gettodate())
    except Exception as e:
        return {"error": str(e)}
    else:
        return {"data": "correct"}

class Room(BaseModel):
    name: str
    EncryptionText: str
    
@app.post("/addroom")
def addroom(data: Room):
    try:
        add_room(data.name, gettodate(), data.EncryptionText)
    except Exception as e:
        return {"error": str(e)}
    else:
        return {"data": "correct"}

@app.get("/getmessages/{roomid}")
def getmessages(roomid: int):
    try:
        con, cur = start_db()
        cur.execute(f"SELECT * FROM messages WHERE room_id={roomid}")
        messages = cur.fetchall()
        con.close()
    except Exception as e:
        return {"error": str(e)}
    else:
        return {"data": messages}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8764)