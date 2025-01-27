using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace basechatting_client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // message box
            if (roomid.Text == "" || roomkey.Text == "") {
                MessageBox.Show("방 이름과 방 키를 입력해주세요.");
                return;
            } else {
                string message = roomid.Text + "\n" + roomkey.Text;
                MessageBox.Show(message);
                int roomidint = int.Parse(roomid.Text);
                websocketmain websocketmain = new websocketmain(roomidint, roomkey.Text);
                websocketmain.Show();
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
