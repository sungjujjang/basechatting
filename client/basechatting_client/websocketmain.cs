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
using System.Net;
using System.Security.Policy;
using System.Net.Http;
using Newtonsoft.Json.Linq; // JSON 데이터 처리용 라이브러리 (설치 필요)

namespace basechatting_client
{
    public partial class websocketmain : Form
    {

        private int _roomid;
        private string _roomkey;
        private string _nickname;
        public websocketmain(int roomid, string roomkey, string nickname)
        {
            _roomid = roomid;
            _nickname = nickname;
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
            // list box auto scroll bottom
            this.KeyPreview = true;
            this.Text = _roomid + "방, 닉네임 :" + _nickname;
            messagelist.Items.Add("연결 중...");
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            var webSocket = new ClientWebSocket();
            try
            {
                await webSocket.ConnectAsync(new Uri("ws://localhost:8765"), System.Threading.CancellationToken.None);
                // send _roomid:_roomkey
                // send roodid:nickname
                byte[] buffer = Encoding.UTF8.GetBytes(_roomid.ToString() + ":" + _nickname);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                // 웹소켓 응답 대기하고 응답 받아옥
                byte[] receiveaBuffer = new byte[1024];
                var resulta = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveaBuffer), System.Threading.CancellationToken.None);
                string message1 = Encoding.UTF8.GetString(receiveaBuffer, 0, resulta.Count);
                string keyresult = Decrypt(_roomkey, message1);
                // Hello world 가 keyresult 안에 포함되어 있는지
                // message box 출력하기
                if (!keyresult.Contains("Hello world")) {
                    MessageBox.Show("방 키가 올바르지 않습니다." + keyresult);
                    this.Close();
                    return;
                } else {
                    // send websocket go
                    byte[] buffer2 = Encoding.UTF8.GetBytes("go");
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer2), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                    // cjlear messagebox
                    messagelist.Items.Clear();
                    MessageBox.Show("연결 성공");
                    string urls = $"http://localhost:8764/getmessages/{_roomid}";
                    using (HttpClient client = new HttpClient())
                    {
                        try
                        {
                            // GET 요청 보내기
                            HttpResponseMessage response = await client.GetAsync(urls);

                            // 응답이 성공했는지 확인
                            response.EnsureSuccessStatusCode();

                            // 응답 내용을 문자열로 읽기
                            string responseBody = await response.Content.ReadAsStringAsync();

                            // JSON 데이터 처리 (Newtonsoft.Json 사용)
                            var json = JToken.Parse(responseBody);

                            // JSON 출력
                            var messageslist = json["data"];
                            for (int i = 0; i < messageslist.Count(); i++)
                            {
                                try
                                {
                                    string decrypted = Decrypt(_roomkey, messageslist[i][2].ToString());
                                    messagelist.Items.Add(decrypted);
                                }
                                catch (Exception error)
                                {
                                    Console.WriteLine(error.Message);
                                }
                            }
                        }
                        catch (HttpRequestException er)
                        {
                            // 요청 실패 처리
                            MessageBox.Show($"요청 실패 {er.Message}");
                        }
                    }
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
                    try
                    {
                        byte[] receiveBuffer = new byte[1024];
                        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), System.Threading.CancellationToken.None);
                        string message = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                        // message include ['Invalid data', 'Room not found', 'Invalid key']
                        if (message.Contains("Invalid data") || message.Contains("Room not found") || message.Contains("Invalid key"))
                        {
                            MessageBox.Show(message);
                            this.Close();
                            return;
                        }
                        else
                        {
                            string decrypted = Decrypt(_roomkey, message);
                            messagelist.Items.Add(decrypted);
                        }
                        messagelist.TopIndex = messagelist.Items.Count - 1;
                    }
                    catch (Exception errors1)
                    {
                        Console.WriteLine(errors1.Message);

                    }
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
            //MessageBox.Show(message);
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("메세지를 입력해주세요.");
                return;
            }
            string url = "http://localhost:8764/addmessage";

            // 전송할 JSON 데이터
            var jsonData = new
            {
                // int roomid
                roomid = _roomid,
                message = Encrypt(_roomkey, $"{_nickname}: {textBox1.Text}"),
            };

            // JSON 데이터를 문자열로 변환
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);

            // HttpClient 생성
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // JSON 데이터를 HttpContent로 변환
                    StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // POST 요청 보내기 (동기적으로 결과 받기)
                    HttpResponseMessage response = client.PostAsync(url, content).Result;

                    // 응답 상태코드 확인
                    response.EnsureSuccessStatusCode();

                    // 응답 본문을 문자열로 읽기
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    // JSON 응답 파싱
                    var jsonResponse = JToken.Parse(responseBody);

                    // JSON 응답 출력
                    Console.WriteLine("Response:");
                    Console.WriteLine(jsonResponse);
                    textBox1.Text = "";
                }
                catch (HttpRequestException er1)
                {
                    // 요청 실패 처리
                    Console.WriteLine($"Request error: {er1.Message}");
                }
            }
        }

        // if press enter key, send message
        private void websocketmain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(sender, e);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }
    }
}
