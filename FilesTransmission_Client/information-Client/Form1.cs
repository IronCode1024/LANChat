using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace information_Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        IPAddress ip;
        //TcpClient tcpClient;
        //TcpListener listener;
        OpenFileDialog ofd = new OpenFileDialog();
        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        private void Form1_Load(object sender, EventArgs e)
        {
            ip = IPAddress.Any;
            //MessageBox.Show("Test"+ip);
            //Application.DoEvents();//刷新窗体

            this.textBoxIP.Text = GetLocalIP();
            //FlashWindow(this.Handle, true);
        }
        //private static extern void FlashWindow(IntPtr hwnd, bool bInvert);

        //连接按钮单击事件
        private void buttonStart_Click(object sender, EventArgs e)
        {
            ip = IPAddress.Parse(this.textBoxIP.Text);
            IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(this.textBoxPort.Text));

            try
            {
                client.Connect(point);
                this.richTextBoxinfo.AppendText("连接服务器成功：" + point + "- -" + GetSystemTime() + "\r\n");
                byte[] byteArr = Encoding.Unicode.GetBytes(GetLocalIP());
                client.Send(byteArr);

                this.Text = "当前连接的服务器：" + point + "- -" + GetSystemTime();

                Thread the = new Thread(ReceiveMsg);
                the.IsBackground = true;
                the.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接失败--" + ex.Message);
            }


            //tcpClient = new TcpClient();
            //try
            //{
            //    tcpClient.Connect(this.textBoxIP.Text, Convert.ToInt32(this.textBoxPort.Text));
            //    this.richTextBoxinfo1.Text = "连接服务器成功 - -" + GetLocalIP()+"- -" +DateTime.Now.ToShortTimeString() + "\r\n" + this.richTextBoxinfo1.Text;
            //    this.backgroundWorker1.RunWorkerAsync();//连接成功，开启多线程异步操作 DoWork
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("连接失败--"+ex.Message);
            //    throw;
            //}
        }

        string wordsText = null;//储存接收到的消息内容
        public void ReceiveMsg(object tx)//接收服务器消息
        {
            //MessageBox.Show("1111111Test");
            //Socket client_tab = tx as Socket;
            long fileLength = 0;
            while (true)
            {
                int firstReceived = 0;//储存首次收到的信息的长度
                byte[] byteClient = new byte[1024 * 1024];//一兆
                try
                {
                    //获取接收的数据,并存入内存缓冲区  返回一个字节数组的长度
                    if (client != null) firstReceived = client.Receive(byteClient);

                    if (firstReceived > 0)//接受到的长度大于0 说明有信息或文件传来
                    {
                        if (byteClient[0] == 0)//0为文字信息
                        {
                            //wordsText = Encoding.UTF8.GetString(byteClient, 1, firstReceived - 1);//真实有用的文本信息要比接收到的少1(标识符)
                            wordsText = Encoding.Unicode.GetString(byteClient, 0, firstReceived);
                            this.richTextBoxinfo.AppendText(wordsText + "\r\n");
                            //clientPool.Add(this.richTextBoxinfo.Text);
                            //MessageBox.Show("1Test" + wordsText);
                        }
                        if (byteClient[0] == 1)//1为文件名
                        {
                            string fileNameWithLength = Encoding.Unicode.GetString(byteClient, 0, firstReceived);
                            wordsText = fileNameWithLength.Split('-').First(); //文件名
                            fileLength = Convert.ToInt64(fileNameWithLength.Split('-').Last());//文件长度
                            //不知道为什么第一个匹配的字符会乱码，中文的“号会自动隐藏掉，在接收端不会显示出引号，会有一个空格
                            //MessageBox.Show("2Test"+wordsText+":::::"+fileLength);
                        }
                        if (byteClient[0] == 2)//2为文件
                        {
                            string fileNameSuffix = wordsText.Substring(wordsText.LastIndexOf('.')); //文件后缀
                            SaveFileDialog sfDialog = new SaveFileDialog();

                            sfDialog.Title = "另存为";
                            sfDialog.Filter = "(*" + fileNameSuffix + ")|*" + fileNameSuffix + ""; //文件类型
                            sfDialog.FileName = wordsText;

                            //MessageBox.Show("" + sfDialog.Filter + ":::::" + sfDialog.FileName);


                            //如果点击了对话框中的保存文件按钮 
                            if (sfDialog.ShowDialog(this) == DialogResult.OK)
                            {
                                string savePath = sfDialog.FileName; //获取文件的全路径
                                //保存文件
                                int received = 0;
                                long receivedTotalFilelength = 0;
                                bool firstWrite = true;
                                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                                {
                                    while (receivedTotalFilelength < fileLength) //之后收到的文件字节数组
                                    {
                                        if (firstWrite)
                                        {
                                            fs.Write(byteClient, 1, firstReceived - 1); //第一次收到的文件字节数组 需要移除标识符1 后写入文件
                                            fs.Flush();

                                            receivedTotalFilelength += firstReceived - 1;

                                            firstWrite = false;
                                            continue;
                                        }
                                        received = client.Receive(byteClient); //之后每次收到的文件字节数组 可以直接写入文件
                                        fs.Write(byteClient, 0, received);
                                        fs.Flush();

                                        receivedTotalFilelength += received;
                                    }
                                    fs.Close();
                                }

                                string fName = savePath.Substring(savePath.LastIndexOf("\\") + 1); //文件名 不带路径
                                string fPath = savePath.Substring(0, savePath.LastIndexOf("\\")); //文件路径 不带文件名
                                this.richTextBoxinfo.AppendText("接收到： " + GetSystemTime() + "\r\n您成功接收了文件：" + fName + "\r\n保存路径为:" + fPath);
                            }
                        }
                    }


                    //if (byteClient[0] == 0)//1为文本
                    //{
                    //    //byte[] byteClient = new byte[1024 * 1024];
                    //    int byteLength = client.Receive(byteClient);
                    //    string receive = Encoding.Unicode.GetString(byteClient, 0, byteLength);
                    //    this.richTextBoxinfo.Text = this.richTextBoxinfo.Text + "接收到：- -" + GetSystemTime() + "- -\r\n" + receive + "\r\n";
                    //}
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    this.richTextBoxinfo.AppendText("峰哥-系统消息：服务器关闭," + ex.Message);
                    //MessageBox.Show("服务器关闭"+ex.Message,"峰哥提示");
                    break;
                }
            }
        }

        string filePath = null;  //文件的全路径
        string fileName = null;  //文件名称(不包含路径) 
        private void filebtn_Click(object sender, EventArgs e)//选择需要发送的文件
        {
            ofd.Title = "请选择文件";
            ofd.Filter = @"所有文件(*.*)|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fileName = ofd.SafeFileName;//获取选取文件的文件名
                //将选择的文件的文件名路径加载到richTextBoxInput中
                this.richTextBoxInput.Text += "已选文件：" + fileName + "\r\n";
                //this.richTextBoxInput.Text += "已选文件：" + Path.GetFileName(fileName) + "\r\n";
                filePath = ofd.FileName;//获取包含文件名的全路径
                // listSongs.Add(path[i]);
                //listBox1.Items.Add(Path.GetFileName(path[i]));
                //listSongs.Add(path[i]);
                this.buttonSend.Text = "发送文件";
            }
        }

        /// <summary>
        /// 发送文件的方法
        /// </summary>
        /// <param name="fileFullPath">文件全路径(包含文件名称)</param>
        private void SendFile(string fileFullPath)
        {
            //if (string.IsNullOrEmpty(fileFullPath))
            //{
            //    MessageBox.Show(@"请选择需要发送的文件!");
            //    return;
            //}

            try
            {
                //发送文件之前 将文件名字和长度发送过去
                long fileLength = new FileInfo(fileFullPath).Length;
                string totalMsg = string.Format("“{0}-{1}", fileName, fileLength);
                MessageBox.Show("" + totalMsg);
                ClientSendMsg(totalMsg, 1);

                //发送文件
                byte[] buffer = new byte[1024 * 1024];

                using (FileStream fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
                {
                    Progressbar_Form pbf = new Progressbar_Form();
                    pbf.ShowDialog();
                    int readLength = 0;
                    bool firstRead = true;
                    long sentFileLength = 0;
                    while ((readLength = fs.Read(buffer, 0, buffer.Length)) > 0 && sentFileLength < fileLength)
                    {
                        sentFileLength += readLength;
                        if (firstRead)
                        {
                            byte[] firstBuffer = new byte[readLength + 2];
                            firstBuffer[0] = 2; //告诉机器该发送的字节数组为文件
                            Buffer.BlockCopy(buffer, 0, firstBuffer, 1, readLength);

                            client.Send(firstBuffer, 0, readLength + 2, SocketFlags.None);

                            firstRead = false;
                            continue;
                        }
                        //之后发送的均为直接读取的字节流
                        client.Send(buffer, 0, readLength, SocketFlags.None);
                    }
                    fs.Close();
                }
                this.richTextBoxinfo.AppendText("发送： " + GetSystemTime() + "\r\n您发送了文件:" + fileName + "\r\n");
            }
            catch (Exception)
            {
                //this.richTextBoxinfo.AppendText("峰哥-系统消息："+ex.Message);
                Application.DoEvents();//刷新窗体
            }
        }

        /// <summary>
        /// 发送字符串信息到服务端的方法
        /// </summary>
        /// <param name="sendMessage"></param>
        /// <param name="symbol"></param>
        public void ClientSendMsg(string sendMessage, byte symbol)
        {
            //文件名发送时第一个字符也会乱码

            /////////////////不知道为什么第一个匹配的字符会乱码，中文的“号会自动隐藏掉，在接收端不会显示出引号，会有一个空格
            try
            {
                if (this.richTextBoxinfo.Text == "发送")
                {
                    this.richTextBoxinfo.ForeColor = Color.Red;
                }
                //NetworkStream stream = tcpClient.GetStream();//实际发送的数组比实际数输入的数组长度多1，用于存取标识符 故加一
                byte[] byteArray = Encoding.Unicode.GetBytes(sendMessage);
                //byte[] byteArrayClient = new byte[byteArray.Length + 1];
                byteArray[0] = symbol;
                //MessageBox.Show("" + byteArrayClient);
                //stream.Write(byteArray,0,byteArray.Length);
                client.Send(byteArray);
            }
            catch (Exception)
            {
                this.timer1.Start();
                this.textBoxts.Visible = true;
                this.textBoxts.Text = "你当前与服务器处在断开状态，消息发送失败！";
            }
        }

        /// <summary>
        /// 发送按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (this.richTextBoxInput.Text == "")
            {
                this.textBoxts.Text = "发送内容不能为空，请重新输入。";
                this.textBoxts.Visible = true;
                timer1.Start();
                //MessageBox.Show("发送内容不能为空，请重新输入。","峰哥提示",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
            }
            else
            {
                if (this.buttonSend.Text == "发送")
                {
                    string message = "”客户端昵称：" + GetLocalIP() + "- - - - - - -" + GetSystemTime() + "\r\n " + this.richTextBoxInput.Text;
                    ClientSendMsg(message, 0);//调用发送文字方法    传入文本和标识符两个参数

                    //this.richTextBoxinfo.AppendText("发送：" + GetSystemTime() + "\r\n" + this.richTextBoxInput.Text + "\r\n");
                    this.richTextBoxInput.Text = "";
                }
                else if (this.buttonSend.Text == "发送文件")
                {
                    this.buttonSend.Text = "发送";
                    //MessageBox.Show("发送文件");
                    SendFile(filePath);//传入文件全路径
                    this.richTextBoxInput.Text = "";
                }

            }
        }



        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //while (true)//实现重复接收消息
            //{
            //    NetworkStream stream = tcpClient.GetStream();
            //    byte[] byteArray = new byte[1024];

            //    int length = stream.Read(byteArray, 0, 1024);//把字节流里面的字节数组放到byteArray里面，length获取客户端发送过来的字节流的数组的长度

            //    string receive = Encoding.Unicode.GetString(byteArray, 0, length);
            //    this.richTextBoxinfo.Text = "接收到：\r\n“" + receive + "”- -" + DateTime.Now.ToShortTimeString() + "\r\n" + this.richTextBoxinfo.Text;
            //}
        }

        /// <summary>
        /// 获取当前系统时间   年/月/日 时:分:秒
        /// </summary>
        /// <returns></returns>
        public DateTime GetSystemTime()
        {
            //DateTime.Now.ToShortTimeString()//获取系统当前时间
            DateTime SystemTime = new DateTime();
            SystemTime = DateTime.Now;
            return SystemTime;
        }






        /// <summary>
        /// 获取本机IPv4地址
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取本机IP出错:" + ex.Message);
                return "";
            }
        }



        //private void get()
        //{
        //    string path = "G://da.exe";
        //    //接收的文件         
        //    FileStream file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write); //写入文件流
        //    IPAddress ip = IPAddress.Parse(this.textBoxIP.Text);
        //    //Random rd = new Random();
        //    //int bb = rd.Next(1000,9999);
        //    listener = new TcpListener(ip, Convert.ToInt32(this.textBoxPort.Text));
        //    listener.Start();
        //    Socket s1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  //定义Socket并初始化          
        //    try
        //    {
        //        listener.Start();        //开始监听     
        //        s1 = listener.AcceptSocket();            //获取Socket连接          
        //        byte[] data = new byte[10000000];      //定义缓冲区  
        //        int longer = data.Length;
        //        int start = 0;
        //        int mid = 0;
        //        if (s1.Connected)             //确定连接      
        //        {
        //            Console.WriteLine("连接成功");
        //            int count = s1.Receive(data, start, longer, SocketFlags.None);  //把接收到的byte存入缓冲区       
        //            mid += count;
        //            longer -= mid;
        //            while (count != 0)
        //            {
        //                count = s1.Receive(data, mid, longer, SocketFlags.None);
        //                mid += count;
        //                longer -= mid;
        //            }
        //            file.Write(data, 0, 1214134); //写入文件，1214134为文件大小，可以用socket发送获得，代码前面已经说明。   
        //            s1.Close();
        //            file.Close();
        //        }
        //    }
        //    catch (NullReferenceException e)
        //    {
        //        Console.WriteLine("{0}", e);
        //    }
        //}


        /// <summary>
        /// 窗体关闭事件，同时关闭所有的socket和线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                byte[] byteArray = Encoding.Unicode.GetBytes(GetLocalIP() + "与服务器断开连接");
                //stream.Write(byteArray,0,byteArray.Length);
                client.Send(byteArray);
                client.Close();
                Application.Exit();
            }
            catch (Exception)
            {
                client.Close();
                Application.Exit();
            }
        }

        //private void richTextBoxinfo1_TextChanged(object sender, EventArgs e)
        //{

        //}

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.textBoxts.Visible = false;
            timer1.Stop();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 form = new Form2();
            form.Show();
        }



    }
}
