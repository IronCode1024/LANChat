using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace information_Client
{
    public partial class Form2 : Form
    {
        static int num = 1;

        public static int Num
        {
            get { return num; }
            set { num = value; }
        }
        int port = 9500;//端口号
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //try
            //{
            //    string path = Application.StartupPath + "\\config.txt";
            //    StreamReader sr = new StreamReader(path, Encoding.Default);
            //    Num = int.Parse(sr.ReadLine());
            //    sr.Close();
            //}
            //catch { }

            //启动接收
            Thread th = new Thread(new ThreadStart(ReceiveMsg));
            th.IsBackground = true;
            th.Start();
        }
        //接收广播
        public void ReceiveMsg()
        {
            IPAddress ip = IPAddress.Any;//要给所有人发送的IP
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//创建发送对象
            IPEndPoint ipe = new IPEndPoint(ip, port);
            server.Bind(ipe);
            byte[] buffer = new byte[60001];
            MemoryStream ms = new MemoryStream();
            while (true)
            {
                int i = server.Receive(buffer);
                //....
                if (buffer[i - 1] == num) //同频道的
                {
                    ms.Write(buffer, 0, i - 1);
                    if (buffer[0] == 100 && i == 2) //结束
                    {
                        Bitmap b2 = new Bitmap(ms);
                        pictureBox1.Image = b2;
                        //ms.Close();
                        ms = new MemoryStream();
                    }
                }

            }
        }
    }
}
