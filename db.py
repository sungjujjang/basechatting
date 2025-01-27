import sqlite3

con = sqlite3.connect('db.db')
cur = con.cursor()

con.execute('''CREATE TABLE IF NOT EXISTS rooms (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, date TEXT, EncryptionText TEXT)''')
con.execute('''CREATE TABLE IF NOT EXISTS messages (id INTEGER PRIMARY KEY AUTOINCREMENT, room_id INTEGER, message TEXT, date TEXT)''')

con.commit()
con.close()