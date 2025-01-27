import sqlite3
import datetime
from Crypto.Cipher import AES
from Crypto.Random import get_random_bytes
from Crypto.Util.Padding import pad, unpad
import base64, string, random

BASE_TEXT = "Hello World"

def add_message(room_id, message, date):
    con, cur = start_db()
    con.execute(f'''INSERT INTO messages (room_id, message, date) VALUES ({room_id}, "{message}", "{date}")''')
    con.commit()
    con.close()
    
def get_room(id):
    con, cur = start_db()
    cur.execute(f"SELECT * FROM rooms WHERE id={id}")
    room = cur.fetchone()
    con.close()
    return room

def start_db():
    con = sqlite3.connect('db.db')
    cur = con.cursor()
    return con, cur

def add_room(name, date, EncryptionText):
    con, cur = start_db()
    con.execute(f'''INSERT INTO rooms (name, date, EncryptionText) VALUES ("{name}", "{date}", "{EncryptionText}")''')
    con.commit()
    con.close()
    
def gettodate():
    return datetime.datetime.now().strftime("%Y%m%d%H%M%S")

def make_key():
    return ''.join(random.choices(string.ascii_letters + string.digits, k=32))

def aes_encrypt(plaintext, key):
    """
    AES-256 암호화 함수
    :param plaintext: 암호화할 텍스트 (문자열)
    :param key: 암호화 키 (32바이트 문자열 또는 바이트)
    :return: 암호화된 데이터 (base64 인코딩 문자열)
    """
    if isinstance(key, str):
        key = key.encode()  # 문자열 키를 바이트로 변환
    if len(key) != 32:
        raise ValueError("키 길이는 32바이트여야 합니다.")

    iv = get_random_bytes(16)  # AES는 16바이트 IV 사용
    cipher = AES.new(key, AES.MODE_CBC, iv)
    ciphertext = cipher.encrypt(pad(plaintext.encode(), AES.block_size))
    encrypted_data = base64.b64encode(iv + ciphertext).decode('utf-8')  # IV와 암호문을 Base64로 인코딩
    return encrypted_data

# AES-256 복호화 함수
def aes_decrypt(encrypted_data, key):
    """
    AES-256 복호화 함수
    :param encrypted_data: 암호화된 데이터 (base64 인코딩 문자열)
    :param key: 복호화 키 (32바이트 문자열 또는 바이트)
    :return: 복호화된 텍스트 (문자열)
    """
    if isinstance(key, str):
        key = key.encode()  # 문자열 키를 바이트로 변환
    if len(key) != 32:
        raise ValueError("키 길이는 32바이트여야 합니다.")

    encrypted_data = base64.b64decode(encrypted_data)  # Base64 디코딩
    iv = encrypted_data[:16]  # 앞 16바이트는 IV
    ciphertext = encrypted_data[16:]  # 나머지는 암호문
    cipher = AES.new(key, AES.MODE_CBC, iv)
    plaintext = unpad(cipher.decrypt(ciphertext), AES.block_size).decode('utf-8')
    return plaintext