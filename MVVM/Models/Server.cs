using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Windows.Input;


namespace test_chat.MVVM.Models
{
    internal class Server
    {
        const string HANDSHAKEHEADER = "00000handshake initiation00000";
        readonly string LOCALIP;
        readonly int PORT;

        private TcpClient tcpClient;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private TcpListener listener;

        public List<Chat> Connections = new List<Chat>();
        public Chat CurrentConnection;

        public delegate void AsyncEventHandler(string line);
        public event AsyncEventHandler? MessageReceived_Event;

        public Server(string localIp, ushort port)
        { 
            LOCALIP = localIp;
            PORT = port;
        }

        public void Connect(string ip)
        {
            tcpClient = new TcpClient(IPAddress.Parse(ip).AddressFamily);
            tcpClient.Connect(IPAddress.Parse(ip), PORT);
            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream());
            streamWriter.AutoFlush = true;

            CurrentConnection = new Chat(ip);
            CurrentConnection.GenerateAES();
            Connections.Add(CurrentConnection);

            Console.WriteLine($"{DateTime.Now}[LOG]: Connected {CurrentConnection.receiverIp}");
            SendMessage(HANDSHAKEHEADER + "");
        }

        public async void SendMessage(string message)
        {
            await streamWriter.WriteLineAsync(message);
            await streamWriter.WriteLineAsync();

            await Console.Out.WriteLineAsync($"{DateTime.Now}[LOG]: Send");
        }

        public async void StartReceiving()
        {
            listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            await Console.Out.WriteLineAsync($"{DateTime.Now}[LOG]: Started");
            var client = await listener.AcceptTcpClientAsync();

            while (client.Connected)
            {
                streamReader = new StreamReader(client.GetStream());
                var line = await streamReader.ReadLineAsync();

                if (!MessageHandler.IsHandshakeRequest(line, HANDSHAKEHEADER))
                {
                    await Console.Out.WriteLineAsync("\n" + line);
                    MessageReceived_Event.Invoke(line);
                    //await Task.Delay(5);
                }


            }
        }
    }

    public static class MessageHandler
    {
        public static bool IsHandshakeRequest(string line, string HSHeader)
        {
            if (line.Length >= HSHeader.Length)
            {
                for (int i = 0; i < HSHeader.Length; i++)
                {
                    if (!(line[i] == HSHeader[i])) { break; }
                }
                return true;
            }
            return false;
        }

        public static string EncryptMessageSync(string message, Aes aes)
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message); // is ASCII better?

            return Encoding.UTF8.GetString(aes.EncryptEcb(byteMessage, PaddingMode.PKCS7));
        }

        public static string DecryptMessageSync(string message, Aes aes) 
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);
            
            return Encoding.UTF8.GetString(aes.DecryptEcb(byteMessage, PaddingMode.PKCS7));
        }

        public static string EncryptMessageAsync(string message, RSAParameters key)
        {
            RSA rsa = RSA.Create();
            rsa.ImportParameters(key);
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);

            return Encoding.UTF8.GetString(rsa.Encrypt(byteMessage, RSAEncryptionPadding.OaepSHA1));
        }

        public static string DecryptMessageAsync(string message, RSAParameters key)
        {
            RSA rsa = RSA.Create();
            rsa.ImportParameters(key);
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);

            return Encoding.UTF8.GetString(rsa.Decrypt(byteMessage, RSAEncryptionPadding.OaepSHA1));
        }
    }
}
