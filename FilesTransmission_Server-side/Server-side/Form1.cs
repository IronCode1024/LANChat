using System;
using System.Collections;
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
using System.Net.WebSockets;
using System.IO;
using System.Threading;

namespace Server_side
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;

            richTextBoxInput.AllowDrop = true;  //允许控件拖放操作
            richTextBoxInput.DragEnter += new DragEventHandler(richTextBoxInput_DragEnter);  //添加页面拖拽事件
            richTextBoxInput.DragDrop += new DragEventHandler(richTextBoxInput_DragDrop);
        }
        private void richTextBoxInput_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void richTextBoxInput_DragDrop(object sender, DragEventArgs e)
        {
            //Array arrayFileName = (Array)e.Data.GetData(DataFormats.FileDrop);

            //string strFileName = arrayFileName.GetValue(0).ToString();

            //StreamReader sr = new StreamReader(strFileName, System.Text.Encoding.Default);
            //richTextBoxInput.Text = sr.ReadToEnd();
            //sr.Close();

            //获取拖放数据
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] content = (string[])e.Data.GetData(DataFormats.FileDrop);
                for (int i = 0; i < content.Length; i++)
                {
                    //这是全路径
                    this.richTextBoxInput.Text += content[i];   
                }
            }
        }




        IPAddress ip;//监听服务器主机的ip
        //TcpListener listener;
        //TcpClient tcpClient;
        //Socket socketSent;
        List<string> listSongs = new List<string>();//创建list表 储存数据到listbox  连接的客户端的ip：port

        OpenFileDialog ofd = new OpenFileDialog();//打开文件对话框

        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建Socket套接字
        //页面加载事件
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Connect_number.Text = null;
            //获取本地ip地址到ip文本框
            this.textBoxIP.Text = GetLocalIP();


            //SetLineSpace(txt_content, 300);
            this.AllowDrop = true;//允许拖拽文件到窗体
            this.richTextBoxinfo.SelectionStart = this.richTextBoxinfo.TextLength;
            this.richTextBoxinfo.ScrollToCaret();
        }
        //启动按钮点击事件
        private void buttonStart_Click(object sender, EventArgs e)
        {
            //this.backgroundWorker1.RunWorkerAsync();//开启backgroundWorker异步 控件

            ip = IPAddress.Parse(this.textBoxIP.Text);
            IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(this.textBoxPort.Text));

            socket.Bind(point);//监听point
            socket.Listen(100);//队列的长度
            //DateTime.Now.ToShortTimeString()//获取系统当前时间
            this.richTextBoxinfo.AppendText("服务器启动成功： " + DateTime.Now.ToShortTimeString() + "\r\n");
            //连接成功 为此客户端创建一个新的线程
            Thread thread = new Thread(AcceptInfo);
            thread.IsBackground = true;
            thread.Start(socket);
            //Broadcast();

            this.buttonStart.Text = "服务已启动";
            this.buttonStart.Enabled = false;
        }

        Dictionary<string, Socket> dic = new Dictionary<string, Socket>();//记录通信用的socket 
        string point;//获取连接的客户端的ip地址 全局变量
        //建立新的Socket连接  函数
        /// <summary>
        /// 连接服务器与客户端方法
        /// </summary>
        /// <param name="tx"></param>
        public void AcceptInfo(object tx)
        {
            Socket socket = tx as Socket;
            while (true)
            {
                try
                {
                    //创建通信是用的Socket
                    Socket tsocket = socket.Accept();
                    point = tsocket.RemoteEndPoint.ToString();//获取连接的客户端的ip地址
                    this.listBox1.Items.Add(point);//把每一次连接的客户端ip放到listBox控件中去
                    this.Connect_number.Text = this.listBox1.Items.Count.ToString();//显示连接的客户端数量

                    this.richTextBoxinfo.AppendText("连接成功 - - " + DateTime.Now.ToShortTimeString() + "连接客户端IP：" + point + "- -\r\n");
                    dic.Add(point, tsocket);

                    //接收消息               接收消息的函数
                    Thread the = new Thread(ReceiveMsg);
                    the.IsBackground = true;
                    the.Start(tsocket);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    break;
                }
            }

        }
        string wordsText = null;//储存接收到的消息内容
        /// <summary>
        /// 接收客户端发来的消息函数
        /// </summary>
        /// <param name="tx"></param>
        public void ReceiveMsg(object tx)
        {
            Socket client = tx as Socket;
            long fileLength = 0;
            while (true)
            {
                int firstReceived = 0;//储存首次收到的信息的长度
                byte[] byteClient = new byte[1024 * 1024];//一兆
                try
                {
                    if (dic.Count > 0)
                    {
                        foreach (var item in dic)//遍历储存通信的socket套接字的字典
                        {
                            if (socket.Poll(1000, SelectMode.SelectRead))
                            {
                                int num = client.Receive(byteClient);
                                if (num == 0)
                                {
                                    MessageBox.Show("断开");
                                }
                            }
                            //MessageBox.Show("Test" + item.Key + item.Value);

                        }
                    }

                    //获取接收的数据,并存入内存缓冲区  返回一个字节数组的长度
                    if (client != null) firstReceived = client.Receive(byteClient);

                    if (firstReceived > 0)//接受到的长度大于0 说明有信息或文件传来
                    {
                        if (byteClient[0] == 0)//0为文字信息
                        {
                            //wordsText = Encoding.UTF8.GetString(byteClient, 1, firstReceived - 1);//真实有用的文本信息要比接收到的少1(标识符)
                            wordsText = Encoding.Unicode.GetString(byteClient, 0, firstReceived);
                            this.richTextBoxinfo.AppendText("接收到： " + GetSystemTime() + "\r\n" + wordsText + "\r\n");

                            ClientSendMsg(wordsText, 0);//向所的客户端广播某一个客户端发来的消息   实现群聊
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



                    //listBox1.Items.Add("已连接客户端IP：" + wordstext);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    //客户端关闭时产生的异常
                    this.richTextBoxinfo.AppendText("\r\n系统消息：" + ex.Message + "\r\n");
                    //MessageBox.Show(ex.Message);
                    this.richTextBoxinfo.AppendText(point + "与服务器断开连接\r\n");
                    //client.Disconnect(true);
                    //dic.Remove(client);
                    this.Connect_number.Text = (this.listBox1.Items.Count - 1).ToString();
                    for (int i = 0; i < this.listBox1.Items.Count; i++)
                    {
                        //删除显示在listbox控件中断开连接的客户端ip
                        if (listBox1.Items[i].ToString() == point)
                        {
                            //MessageBox.Show(point + ":" + i);
                            listBox1.Items.RemoveAt(i);
                        }
                    }

                    //foreach (var item in dic)//遍历储存通信的socket套接字的字典
                    //{
                    //    MessageBox.Show("Test" + item.Key + item.Value);

                    //}


                    break;
                }
            }
        }

        //异步工作
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //try
            //{

            //    this.buttonStart.Enabled = false;
            //    ip = IPAddress.Parse(this.textBoxIP.Text);
            //    //Random rd = new Random();
            //    //int bb = rd.Next(1000,9999);

            //    listener = new TcpListener(ip, Convert.ToInt32(this.textBoxPort.Text));
            //    listener.Start();
            //    //sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //    this.richTextBoxinfo2.Text = "服务器启动成功 - - " + DateTime.Now.ToShortTimeString() + "\r\n" + this.richTextBoxinfo2.Text;
            //    tcpClient = listener.AcceptTcpClient();//中断  等待

            //    this.richTextBoxinfo2.Text = "连接成功 - - 连接客户端IP：" + GetLocalIP() + "- -" + DateTime.Now.ToShortTimeString() + "\r\n" + this.richTextBoxinfo2.Text;
            //    //this.listBox1.Text = "已连接客户端IP：" + GetLocalIP();
            //    listBox1.Items.Add("已连接客户端IP：" + GetLocalIP());



            //    NetworkStream stream = tcpClient.GetStream();
            //    while (true)//实现重复接收消息
            //    {
            //        byte[] byteArray = new byte[1024];

            //        int length = stream.Read(byteArray, 0, 1024);//把字节流里面的字节数组放到byteArray里面，length获取客户端发送过来的字节流的数组的长度

            //        string receive = Encoding.Unicode.GetString(byteArray, 0, length);
            //        this.richTextBoxinfo.Text = "接收到：\r\n“" + receive + "”- -" + DateTime.Now.ToShortTimeString() + "\r\n" + this.richTextBoxinfo.Text;
            //    }
            //}
            //catch (Exception er)
            //{
            //    MessageBox.Show("错误：" + er.Message);
            //    this.richTextBoxinfo.AppendText("远程服务器异常" + "\r\n");
            //    this.buttonStart.Text = "启动";
            //    this.buttonStart.Enabled = true;
            //}

        }

        string filePath = null;  //文件的全路径
        string fileName = null;  //文件名称(不包含路径) 
        //选择文件按钮单击事件
        private void buttonFiles_Click(object sender, EventArgs e)
        {
            ofd.Title = "请选择文件";
            ofd.Filter = @"所有文件(*.*)|*.*";
            ofd.Multiselect = true;

            //this.richTextBoxInput.Text += ofd.FileName+"\r\n";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                //string[] path = ofd.FileNames;
                fileName = ofd.SafeFileName;//获取选取文件的文件名
                //将选择的文件的文件名路径加载到richTextBoxInput中
                this.richTextBoxInput.Text += "已选文件：" + fileName + "\r\n";
                //this.richTextBoxInput.Text += "已选文件：" + Path.GetFileName(path[i]) + "\r\n";
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
            string ipclient;
            for (int i = 0; i < listBox1.Items.Count; i++)//遍历listbox里的 客户端的ip 数据
            {
                ipclient = (this.listBox1.Items[i]).ToString();//获取listbox里面的数据
                //发送文件之前 将文件名字和长度发送过去
                long fileLength = new FileInfo(fileFullPath).Length;
                string totalMsg = string.Format("“{0}-{1}", fileName, fileLength);
                ClientSendMsg(totalMsg, 1);


                //发送文件
                byte[] buffer = new byte[1024 * 1024];

                using (FileStream fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
                {
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

                            dic[ipclient].Send(firstBuffer, 0, readLength + 2, SocketFlags.None);
                            firstRead = false;
                            continue;
                        }
                        //之后发送的均为直接读取的字节流
                        dic[ipclient].Send(buffer, 0, readLength, SocketFlags.None);
                        MessageBox.Show("刘");
                    }
                    fs.Close();
                }
            }
            this.richTextBoxinfo.AppendText("发送：" + GetSystemTime() + "\r\n您发送了文件:" + fileName + "\r\n");
        }
        /// <summary>
        /// 发送字符串信息到客户端的方法  群发服务器接收到的消息到每一个在线的客户端
        /// </summary>
        /// <param name="sendMessage">要发送的文本内容</param>
        /// <param name="symbol">储存发送的是什么文件的标记，文字消息为0</param>
        public void ClientSendMsg(string sendMessage, byte symbol)
        {
            /////////////////不知道为什么第一个匹配的字符会乱码，中文的“号会自动隐藏掉，在接收端不会显示出引号，会有一个空格
            //文件名发送时第一个字符也会乱码
            string ipclient;
            //发送消息到每一个客户端
            for (int i = 0; i < listBox1.Items.Count; i++)//遍历listbox里的 客户端的ip 数据
            {
                //    MessageBox.Show("Test111111:::" + listBox1.Items.Count);
                ipclient = (this.listBox1.Items[i]).ToString();//获取listbox里面的数据
                byte[] byteArray = Encoding.Unicode.GetBytes(sendMessage);//字节流
                byteArray[0] = symbol;
                dic[ipclient].Send(byteArray);//把ipclient发送出去  ，把消息发送到每一个连接到服务器的客户端
                ////dic为储存连接的客户端socket套接字信息
                //socket.Send(byteArray);
                //MessageBox.Show("Test"+byteArray[0]);
            }
        }

        //发送单击事件
        private void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.richTextBoxInput.Text == "")
                {
                    this.textBoxts.Visible = true;
                    timer1.Start();
                    //MessageBox.Show("发送内容不能为空，请重新输入。","峰哥提示",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
                }
                else
                {
                    if (this.buttonSend.Text == "发送")
                    {
                        //foreach (var item in dic)//遍历储存通信的socket套接字的字典
                        //{
                        //    MessageBox.Show("Test" + item.Key + item.Value);
                        //}

                        //buttonSend.Text = "发送";
                        string message = this.richTextBoxInput.Text;
                        this.richTextBoxinfo.AppendText("发送： " + GetSystemTime() + "\r\n" + this.richTextBoxInput.Text + "\r\n");
                        string ipclient;
                        //发送消息到每一个客户端
                        for (int i = 0; i < listBox1.Items.Count; i++)//遍历listbox里的 客户端的ip 数据
                        {
                            //    MessageBox.Show("Test111111:::" + listBox1.Items.Count);
                            ipclient = (this.listBox1.Items[i]).ToString();//获取listbox里面的数据
                            byte[] byteArray = Encoding.Unicode.GetBytes("“服务器消息：" + message);//字节流
                            byteArray[0] = 0;
                            dic[ipclient].Send(byteArray);//把ipclient发送出去  ，把消息发送到每一个连接到服务器的客户端
                            ////dic为储存连接的客户端socket套接字信息
                            //socket.Send(byteArray);
                            //MessageBox.Show("Test"+byteArray[0]);
                        }

                        this.richTextBoxInput.Text = "";
                        //this.buttonSend.Text = "发送文件";
                    }
                    else if (this.buttonSend.Text == "发送文件")
                    {
                        this.buttonSend.Text = "发送";
                        SendFile(filePath);

                        this.richTextBoxInput.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("我的错误：" + ex.Message, "峰哥提示");
            }
            //socket.Send(byteArray);
        }

        //文件拖拽到窗体中响应事件
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            //获取拖放数据
            //if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //    string[] content = (string[])e.Data.GetData(DataFormats.FileDrop);
            //    for (int i = 0; i < content.Length; i++)
            //    {
            //        这是全路径
            //        this.richTextBoxInput.Text += content[i];
            //    }
            //}
        }

        private void richTextBoxinfo_Layout(object sender, LayoutEventArgs e)
        {
            //this.richTextBoxinfo.SelectionStart = this.richTextBoxinfo.TextLength;
            //this.richTextBoxinfo.ScrollToCaret();
        }
        //点击enter键发送消息
        private void richTextBoxInput_KeyPress(object sender, KeyPressEventArgs e)
        {


        }

        private void richTextBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Enter)
            //{
            //    string message = this.richTextBoxInput.Text;
            //    this.richTextBoxinfo2.Text = "发送：\r\n“" + message + "”- -" + DateTime.Now.ToShortTimeString() + "\r\n" + this.richTextBoxinfo2.Text;
            //    NetworkStream stream = tcpClient.GetStream();
            //    byte[] byteArray = Encoding.Unicode.GetBytes(message);
            //    stream.Write(byteArray, 0, byteArray.Length);
            //}
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


        private void richTextBoxinfo_TextChanged(object sender, EventArgs e)
        {
            //string client;
            //try
            //{
            //    for (int i = 0; i < listBox1.Items.Count; i++)//遍历listbox里的 客户端的ip 数据
            //    {
            //        client = (this.listBox1.Items[i]).ToString();//获取listbox里面的数据
            //        byte[] byteArray = Encoding.Unicode.GetBytes(wordsText);//字节流
            //        dic[client].Send(byteArray);//把ipclient发送出去  ，把消息发送到每一个连接到服务器的客户端
            //    }
            //}
            //catch (Exception)
            //{

            //    try
            //    {
            //        for (int i = 0; i < listBox1.Items.Count; i++)//遍历listbox里的 客户端的ip 数据
            //        {
            //            client = (this.listBox1.Items[i]).ToString();//获取listbox里面的数据
            //            byte[] byteArray = Encoding.Unicode.GetBytes(point + "与服务器断开连接\r\n");//字节流
            //            dic[client].Send(byteArray);//把ipclient发送出去  ，把消息发送到每一个连接到服务器的客户端
            //        }
            //    }
            //    catch (Exception)
            //    {

            //    }
            //}
        }
        /// <summary>
        /// 获取当前系统时间    年/月/日 时:分:秒
        /// </summary>
        /// <returns></returns>
        public DateTime GetSystemTime()
        {
            //DateTime.Now.ToShortTimeString()//获取系统当前时间
            DateTime SystemTime = new DateTime();
            SystemTime = DateTime.Now;
            return SystemTime;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.textBoxts.Visible == true)
            {
                this.textBoxts.Visible = false;
                timer1.Stop();
            }



        }
        /// <summary>
        /// 当删除发送文件的文件路径名的时候 发送按钮变为发送文字样式 此时不在发送文件  只会把文件路径名当成文字发送出去
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBoxInput_TextChanged(object sender, EventArgs e)
        {
            this.buttonSend.Text = "发送";

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 form = new Form2();
            form.Show();

            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //byte[] byteArray = Encoding.Unicode.GetBytes(GetLocalIP() + "与服务器断开连接");
                //stream.Write(byteArray,0,byteArray.Length);
                ClientSendMsg((GetLocalIP() + "与服务器断开连接"),0);

                string ipclient;
                //发送消息到每一个客户端
                for (int i = 0; i < listBox1.Items.Count; i++)//遍历listbox里的 客户端的ip 数据
                {
                    //    MessageBox.Show("Test111111:::" + listBox1.Items.Count);
                    ipclient = (this.listBox1.Items[i]).ToString();//获取listbox里面的数据
                    byte[] byteArray = Encoding.Unicode.GetBytes("“服务器消息：" + "服务器关闭！" + GetSystemTime());//字节流
                    byteArray[0] = 0;
                    dic[ipclient].Send(byteArray);//把ipclient发送出去  ，把消息发送到每一个连接到服务器的客户端
                }

                //socket.Send(byteArray);
                socket.Close();
                Application.Exit();

            }
            catch (Exception)
            {
                socket.Close();
                Application.Exit();
            }
        }
    }
}
