using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace basechatting_client
{
    public partial class websocketmain : Form
    {

        private int _roomid;
        private string _roomkey;
        public websocketmain(int roomid, string roomkey)
        {
            _roomid = roomid;
            _roomkey = roomkey;
            InitializeComponent();
        }

        public static string Encrypt(string key, string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32)); // 키 길이 조정
                aesAlg.GenerateIV(); // IV를 랜덤으로 생성
                byte[] iv = aesAlg.IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, iv);

                using (MemoryStream ms = new MemoryStream())
                {
                    // IV를 암호문 앞에 포함
                    ms.Write(iv, 0, iv.Length);
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray()); // Base64로 인코딩하여 반환
                }
            }
        }

        // AES 복호화 함수 (IV를 추출하여 사용)
        public static string Decrypt(string key, string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32)); // 키 길이 조정

                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                // 암호문에서 IV 추출
                byte[] iv = new byte[16];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);

                byte[] cipherTextBytes = new byte[cipherBytes.Length - iv.Length];
                Array.Copy(cipherBytes, iv.Length, cipherTextBytes, 0, cipherTextBytes.Length);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, iv);

                using (MemoryStream ms = new MemoryStream(cipherTextBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        private async void websocketmain_Load(object sender, EventArgs e)
        {
            messagelist.Items.Add("연결 중...");
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            var webSocket = new ClientWebSocket();
            try
            {
                await webSocket.ConnectAsync(new Uri("ws://localhost:8765"), System.Threading.CancellationToken.None);
                // send _roomid:_roomkey
                byte[] buffer = Encoding.UTF8.GetBytes(_roomid.ToString());
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                // 웹소켓 응답 대기하고 응답 받아옥
                byte[] receiveaBuffer = new byte[1024];
                var resulta = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveaBuffer), System.Threading.CancellationToken.None);
                string message1 = Encoding.UTF8.GetString(receiveaBuffer, 0, resulta.Count);
                string keyresult = Decrypt(_roomkey, message1);
                // Hello world 가 keyresult 안에 포함되어 있는지
                // message box 출력하기
                MessageBox.Show(keyresult);
                if (!keyresult.Contains("Hello world")) {
                    MessageBox.Show("방 키가 올바르지 않습니다." + keyresult);
                    this.Close();
                    return;
                } else {
                    // send websocket go
                    byte[] buffer2 = Encoding.UTF8.GetBytes("go");
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer2), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                    messagelist.Items.Add("연결 성공");
                }
                // 웹소켓 연결 끊어졌다면 메세지 박스 표시하고 창 닫기
                if (webSocket.State != WebSocketState.Open)
                {
                    MessageBox.Show("연결이 올바르지 않습니다.");
                    this.Close();
                    return;
                }

                // 메시지 받고 리스트박스에 표시 무한반복
                while (webSocket.State == WebSocketState.Open)
                {
                    byte[] receiveBuffer = new byte[1024];
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), System.Threading.CancellationToken.None);
                    string message = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                    messagelist.Items.Add(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"연결 중 오류가 발생했습니다: {ex.Message}");
                this.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string message = _roomid + "\n" + _roomkey;
            MessageBox.Show(message);
            this.Close();
        }

    }
}
