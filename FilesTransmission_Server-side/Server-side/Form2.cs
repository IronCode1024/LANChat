using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;

namespace Server_side
{
    public partial class Form2 : Form
    {
        public int Num = 1;//频道
        //准备两张图片，一张用于预览一张用于发送
        Graphics g;//创建一个画布
        Bitmap bmp;//准备一个图片
        int port = 9500;//准备一个端口
        Graphics g2;
        Bitmap bmp2;
        public Form2()
        {
            InitializeComponent();
            Rectangle rec= Screen.PrimaryScreen.Bounds;//获取以屏幕边框为矩形
           bmp = new Bitmap(rec.Width, rec.Height);//设置图片的大小为屏幕大小
           g = Graphics.FromImage(bmp);//以图片创建画布
           pictureBox1.Image = bmp;//把图片显示到picture上
           Form.CheckForIllegalCrossThreadCalls = false;//设置跨线程访问控件不监控
            //用于发送
           bmp2 = new Bitmap(rec.Width, rec.Height);
           g2 = Graphics.FromImage(bmp2);
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            //新建一个线程执行发送的任务
            Thread th = new Thread(new ThreadStart(SendData));
            th.IsBackground = true;
            th.Start();
        }
        public void SendData()
        {
            IPAddress ip = IPAddress.Broadcast;//要给所有人发送的IP
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//创建发送对象
            server.Connect(ip, port);//创建连接对象
            while (true)
            {
                //1.截图
                //用于预览的图片
                Thread.Sleep(200);
                g.Clear(Color.White);
                Size size = new System.Drawing.Size(bmp.Width, bmp.Height);
                g.CopyFromScreen(0, 0, 0, 0, size);

                int a = Cursor.Position.X;
                int b = Cursor.Position.Y;
                // Cursor.Current.
                this.Text = "x" + a + "y" + b;
                g.FillEllipse(new SolidBrush(Color.Red), new Rectangle(a, b, 15, 15));

                // Cursor.Current.Draw(g, new Rectangle(a, b, 100, 100));
                pictureBox1.Refresh();//现在自己的电脑上绘制出屏幕
                //用于发送的图片
                g2.Clear(Color.White);
                g2.CopyFromScreen(0, 0, 0, 0, new Size(bmp2.Width, bmp2.Height));

                g2.FillEllipse(new SolidBrush(Color.Red), new Rectangle(a, b, 15, 15));
                //2.发送

                //内存流，在内存中存在的流，不依赖于磁盘文件
                MemoryStream ms = new MemoryStream();
                bmp2.Save(ms, ImageFormat.Jpeg);//将图片保存在内存中
                //将ms流中所有的内容拿出来
                byte[] buffer = ms.GetBuffer();//将内存流中的内容一次性拿出来s
                int sendLength = 60000;//每次发送60000个字节
                int times = buffer.Length / sendLength;
                if (buffer.Length % sendLength != 0)
                {
                    times++;
                }
                byte[] b2 = new byte[60001];
                b2[60000] = (byte)Num;
                for (int i = 0; i < times - 1; i++)
                {
                    Array.Copy(buffer, i * sendLength, b2, 0, sendLength);
                    server.Send(b2, 0, sendLength + 1, SocketFlags.None);//发送sendLength个字节
                }
                b2 = new byte[buffer.Length - (times - 1) * sendLength + 1];
                Array.Copy(buffer, (times - 1) * sendLength, b2, 0, buffer.Length - (times - 1) * sendLength);
                b2[buffer.Length - (times - 1) * sendLength] = (byte)Num;
                server.Send(b2, 0, buffer.Length - (times - 1) * sendLength + 1, SocketFlags.None);//不足sendLength的最后依次发送
                byte[] buf2 = new byte[2];//作为标识字节，当对方收到这个字节，说明这个张图片已经发送完成
                buf2[0] = 100;//结束
                buf2[1] = (byte)Num;//频道
                server.Send(buf2, 0, buf2.Length, SocketFlags.None);

            }
        }
    }
}
