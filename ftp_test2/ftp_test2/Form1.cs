using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace ftp_test2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //FTP操作，实例化FTPFactory的对象
        private FTPFactory _ftp = new FTPFactory();
        private List<string> ftpFileList;
        // 连接
        private void button3_Click(object sender, EventArgs e)
        {
            _ftp.setRemoteHost(textBox1.Text);
            _ftp.getRemoteHost();
            _ftp.setRemotePort(int.Parse(textBox2.Text));
            _ftp.getRemotePort();
            _ftp.setRemoteUser(textBox4.Text);
            _ftp.setRemotePass(textBox5.Text);
            if (button3.Text == "连接")
            {
                
                _ftp.login();
                if (_ftp.logined)
                {
                    button3.Text = "断开";
                    listBox3.Items.Add("状态:	正在连接" + textBox1.Text + "...");
                    listBox3.Items.Add("状态:	正在连接等待欢迎消息...");
                    listBox3.Items.Add("状态:	已登录");
                    freshFileBox_Right();
                }
            }
            else
            {
                _ftp.close();
                if(!_ftp.logined)
                {
                    button3.Text = "连接";
                    listBox3.Items.Add("状态:	连接已关闭");
                }
            }
        }

        
        // 往FTP上传文件
        private void button2_Click(object sender, EventArgs e)
        {
            //初始化FTP
            InitFtp();
            //文件路径
            Directory.SetCurrentDirectory(textBox3.Text);
            string filepath = Directory.GetCurrentDirectory();
            string localfile = listBox1.SelectedItem.ToString();
            _ftp.upload(localfile);
            listBox3.Items.Add("状态：" + localfile + "上传成功");
            freshFileBox_Right();
            _ftp.close();
        }

        
        // 初始化FTP
        private void InitFtp()
        {
            _ftp.setRemoteHost(textBox1.Text);
            _ftp.getRemoteHost();
            _ftp.setRemotePort(int.Parse(textBox2.Text));
            _ftp.getRemotePort();
            _ftp.setRemoteUser(textBox4.Text);
            _ftp.setRemotePass(textBox5.Text);
            _ftp.login();
        }

        //下载
        private void button1_Click(object sender, EventArgs e)
        {
                InitFtp();

                Directory.SetCurrentDirectory(textBox3.Text);
                string filepath = Directory.GetCurrentDirectory();

                string rmfile = listBox2.SelectedItem.ToString();
                string localfile = filepath + "\\" + rmfile;
                _ftp.download(rmfile, localfile);
                listBox3.Items.Add("状态：" + rmfile + "下载成功！");

                freshFileBox_Left();

                _ftp.close();
        }

        //选择本地文件夹
        private void button4_Click(object sender, EventArgs e)
        {
            string path = string.Empty;
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                path = fbd.SelectedPath;
                listBox3.Items.Add("选中本地路径:" + path);
            }

            textBox3.Text = path;
            freshFileBox_Left();

        }

        //更新左边本地文件的listbox
        private void freshFileBox_Left()
        {
            listBox1.Items.Clear();
            if (textBox3.Text == "") return;
            var files = Directory.GetFiles(textBox3.Text, "*.*");
            foreach (var file in files)
            {
                Console.WriteLine(file);
                string[] temp = Regex.Split(file, @"\\");
                listBox1.Items.Add(temp[temp.Length - 1]);
            }
        }

        //更新右边服务器文件的listbox
        private void freshFileBox_Right()
        {
            string ListDir = "";

            listBox2.Items.Clear();

            string mask = _ftp.getRemotePath();
            string[] temp = _ftp.getFileList(mask);
            for (int i = 0; i <= temp.Length - 1; i++)
            {
                ListDir = temp[i] + "\r\n";
                ///*
                MatchCollection k = Regex.Matches(ListDir, @"\S*.txt\b");
                foreach (Match m in k)
                {
                    listBox2.Items.Add(m);
                }
                //*/

                //listBox2.Items.Add(ListDir);

            }
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }
    }
}
