using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;

namespace ftp_test2
{
    class FTPFactory
    {
        private string remoteHost, remotePath, remoteUser, remotePass, mes;
        private int remotePort, bytes;
        private Socket clientSocket;
        private int retValue;
        public Boolean logined;
        private string reply;
        private static int BLOCK_SIZE = 512;
        Byte[] buffer = new Byte[BLOCK_SIZE];
        Encoding ASCII = Encoding.ASCII;

        
        // 构造函数
        public FTPFactory()
        {
            remoteHost = "localhost";
            remotePath = "/";
            remoteUser = "anonymous";
            remotePass = "";
            remotePort = 21;
            logined = false;
        }

        
        // 设置服务器地址
        public void setRemoteHost(string remoteHost)
        {
            this.remoteHost = remoteHost;
        }
        
        // 获取服务器地址
        public string getRemoteHost()
        {
            return remoteHost;
        }

        // 设置端口号
        public void setRemotePort(int remotePort)
        {
            this.remotePort = remotePort;
        }

        
        // 获取端口号
        public int getRemotePort()
        {
            return remotePort;
        }

        /// 设置远程路径
        public void setRemotePath(string remotePath)
        {
            this.remotePath = remotePath;
        }

        
        // 获取远程路径
        public string getRemotePath()
        {
            return remotePath;
        }

        
        // 设置用户名
        public void setRemoteUser(string remoteUser)
        {
            this.remoteUser = remoteUser;
        }

        
        // 设置密码
        public void setRemotePass(string remotePass)
        {
            this.remotePass = remotePass;
        }

        
        // 获取文件列表
        public string[] getFileList(string mask)
        {
            if (!logined)
            {
                login();
            }
            Socket cSocket = createDataSocket();
            sendCommand("LIST " + mask);
            if (!(retValue == 150 || retValue == 125))
            {
                throw new IOException(reply.Substring(4));
            }
            mes = "";
            while (true)
            {
                int bytes = cSocket.Receive(buffer, buffer.Length, 0);
                mes += ASCII.GetString(buffer, 0, bytes);
                if (bytes < buffer.Length)
                {
                    break;
                }
            }
            string[] mess = Regex.Split(mes, "\r\n");
            cSocket.Close();
            readReply();
            if (retValue != 226)
            {
                throw new IOException(reply.Substring(4));
            }
            return mess;

        }

        // 获取文件大小
        public long getFileSize(string fileName)
        {
            if (!logined)
            {
                login();
            }
            sendCommand("SIZE " + fileName);
            long size = 0;
            if (retValue == 213)
            {
                size = Int64.Parse(reply.Substring(4));
            }
            else
            {
                throw new IOException(reply.Substring(4));
            }
            return size;
        }

        // 登陆
        public void login()
        {
            clientSocket = new
            Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(remoteHost), remotePort);
            try
            {
                clientSocket.Connect(ep);
            }
            catch (Exception)
            {
                throw new IOException("连接不上远程服务器！");
            }
            readReply();
            if (retValue != 220)
            {
                close();
                throw new IOException(reply.Substring(4));
            }
            
            sendCommand("USER " + remoteUser);
            if (!(retValue == 331 || retValue == 230))
            {
                cleanup();
                throw new IOException(reply.Substring(4));
            }
            if (retValue != 230)
            {
                
                sendCommand("PASS " + remotePass);
                if (!(retValue == 230 || retValue == 202))
                {
                    cleanup();
                    throw new IOException(reply.Substring(4));
                }
            }
            logined = true;
            Console.WriteLine("地址" + remoteHost + "连接成功！");
            chdir(remotePath);
        }

        
        // 设置通讯模式 true - 二进制模式  false - ASCII码模式
        public void setBinaryMode(Boolean mode)
        {
            if (mode)
            {
                sendCommand("TYPE I");              //二进制模式
            }
            else
            {
                sendCommand("TYPE A");              //ASCII码模式
            }
            if (retValue != 200)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        /// 下载一个文件
        public void download(string remFileName)
        {
            download(remFileName, "", false);
        }
        
        // 下载文件
        public void download(string remFileName, Boolean resume)
        {
            download(remFileName, "", resume);
        }
        
        // 下载文件
        public void download(string remFileName, string locFileName)
        {
            download(remFileName, locFileName, false);
        }

        // 下载文件 路径必须存在
        public void download(string remFileName, string
        locFileName, Boolean resume)
        {
            if (!logined)
            {
                login();
            }
            setBinaryMode(true);
            Console.WriteLine("Downloading file " + remFileName + " from " + remoteHost + "/" + remotePath);
            if (locFileName.Equals(""))
            {
                locFileName = remFileName;
            }
            if (!File.Exists(locFileName))
            {
                Stream st = File.Create(locFileName);
                st.Close();
            }
            FileStream output = new
            FileStream(locFileName, FileMode.Open);
            Socket cSocket = createDataSocket();
            long offset = 0;
            if (resume)
            {
                offset = output.Length;
                if (offset > 0)
                {
                    sendCommand("REST " + offset);
                    if (retValue != 350)
                    {
                        offset = 0;
                    }
                }
                if (offset > 0)
                {
                   
                    long npos = output.Seek(offset, SeekOrigin.Begin);
                    Console.WriteLine("new pos=" + npos);
                }
            }
            sendCommand("RETR " + remFileName);
            if (!(retValue == 150 || retValue == 125))
            {
                throw new IOException(reply.Substring(4));
            }
            while (true)
            {
                bytes = cSocket.Receive(buffer, buffer.Length, 0);
                output.Write(buffer, 0, bytes);
                if (bytes <= 0)
                {
                    break;
                }
            }
            output.Close();
            if (cSocket.Connected)
            {
                cSocket.Close();
            }
            Console.WriteLine("");
            readReply();
            if (!(retValue == 226 || retValue == 250))
            {
                throw new IOException(reply.Substring(4));
            }
        }

        
        // 上传文件
        public void upload(string fileName)
        {
            upload(fileName, false);
        }
        
        // 上传文件
        public void upload(string fileName, Boolean resume)
        {
            if (!logined)
            {
                login();
            }
            Socket cSocket = createDataSocket();
            long offset = 0;
            if (resume)
            {
                try
                {
                    setBinaryMode(true);
                    offset = getFileSize(fileName);
                }
                catch (Exception)
                {
                    offset = 0;
                }
            }
            if (offset > 0)
            {
                sendCommand("REST " + offset);
                if (retValue != 350)
                {
                    offset = 0;
                }
            }
            sendCommand("STOR " + Path.GetFileName(fileName));
            if (!(retValue == 125 || retValue == 150))
            {
                throw new IOException(reply.Substring(4));
            }

            FileStream input = new
            FileStream(fileName, FileMode.Open);
            if (offset != 0)
            {
                
                input.Seek(offset, SeekOrigin.Begin);
            }
            Console.WriteLine("Uploading file " + fileName + " to " + remotePath);
            while ((bytes = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                cSocket.Send(buffer, bytes, 0);
            }
            input.Close();
            Console.WriteLine("");
            if (cSocket.Connected)
            {
                cSocket.Close();
            }
            readReply();
            if (!(retValue == 226 || retValue == 250))
            {
                throw new IOException(reply.Substring(4));
            }
        }

        
        // 删除文件
        public void deleteRemoteFile(string fileName)
        {
            if (!logined)
            {
                login();
            }
            sendCommand("DELE " + fileName);
            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        
        // 重命名文件
        public void renameRemoteFile(string oldFileName, string
        newFileName)
        {
            if (!logined)
            {
                login();
            }
            sendCommand("RNFR " + oldFileName);
            if (retValue != 350)
            {
                throw new IOException(reply.Substring(4));
            }

            sendCommand("RNTO " + newFileName);
            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        
        // 创建一个远程路径
        public void mkdir(string dirName)
        {
            if (!logined)
            {
                login();
            }
            sendCommand("MKD " + dirName);
            if (retValue != 257)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        
        // 删除远程路径
        public void rmdir(string dirName)
        {
            if (!logined)
            {
                login();
            }
            sendCommand("RMD " + dirName);
            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        
        // 改变当前远程路径
        public void chdir(string dirName)
        {
            if (dirName.Equals("."))
            {
                return;
            }
            if (!logined)
            {
                login();
            }
            sendCommand("CWD " + dirName);
            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
            this.remotePath = dirName;
            Console.WriteLine("远程服务器工作路径" + remotePath);
        }

        
        // 关闭链接
        public void close()
        {
            if (clientSocket != null)
            {
                sendCommand("QUIT");
            }
            cleanup();
            
        }

        private void readReply()
        {
            mes = "";
            reply = readLine();
            retValue = Int32.Parse(reply.Substring(0, 3));
        }

        private void cleanup()
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }
            logined = false;
        }
        public string readLine()
        {
            while (true)
            {
                bytes = clientSocket.Receive(buffer, buffer.Length, 0);
                mes += ASCII.GetString(buffer, 0, bytes);
                if (bytes < buffer.Length)
                {
                    break;
                }
            }

            string[] mess = Regex.Split(mes, "\r\n");
            if (mes.Length > 2)
            {
                mes = mess[mess.Length - 2];
            }
            else
            {
                mes = mess[0];
            }
            if (!mes.Substring(3, 1).Equals(" "))
            {
                return readLine();
            }
            
            return mes;
        }
        public byte[] sendCommand(String command)
        {
            Byte[] cmdBytes =
            //Encoding.ASCII.GetBytes((command + "\r\n").ToCharArray());

            Encoding.Default.GetBytes((command + "\r\n").ToCharArray());
            clientSocket.Send(cmdBytes, cmdBytes.Length, 0);
            readReply();
            return cmdBytes;
        }


        public Socket createDataSocket()
        {
            sendCommand("PASV");
            if (retValue != 227)
            {
                throw new IOException(reply.Substring(4));
            }
            int index1 = reply.IndexOf('(');
            int index2 = reply.IndexOf(')');
            string ipData =
            reply.Substring(index1 + 1, index2 - index1 - 1);
            int[] parts = new int[6];
            int len = ipData.Length;
            int partCount = 0;
            string buf = "";
            for (int i = 0; i < len && partCount <= 6; i++)
            {
                char ch = Char.Parse(ipData.Substring(i, 1));
                if (Char.IsDigit(ch))
                    buf += ch;
                else if (ch != ',')
                {
                    throw new IOException("Malformed PASV reply: " +
                    reply);
                }
                if (ch == ',' || i + 1 == len)
                {
                    try
                    {
                        parts[partCount++] = Int32.Parse(buf);
                        buf = "";
                    }
                    catch (Exception)
                    {
                        throw new IOException("Malformed PASV reply: " +
                        reply);
                    }
                }
            }
            string ipAddress = parts[0] + "." + parts[1] + "." +
            parts[2] + "." + parts[3];
            int port = (parts[4] << 8) + parts[5];
            Socket s = new
            Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new
            IPEndPoint(IPAddress.Parse(ipAddress), port);
            try
            {
                s.Connect(ep);
            }
            catch (Exception)
            {
                throw new IOException("无法链接远程服务器");
            }
            return s;
        }
    }
}
