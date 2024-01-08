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
        #region Constants
        const string HANDSHAKE_HEADER = "00000handshake initiation00000";
        const int AES_KEY_LENGTH = 32;
        const int AES_IV_LENGTH = 16;
        const int RSA_PUBLIC_KEY_LENGTH = 270;
        const int RSA_PRIVATE_KEY_LENGTH = 1191;
        readonly string LOCAL_IP;
        readonly int PORT;
        readonly Guid GUID = Guid.NewGuid();
        #endregion

        #region TCP objects
        private TcpClient tcpClient;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private TcpListener listener;
        #endregion

        #region events
        public delegate void AsyncEventHandler(string line);
        public event AsyncEventHandler? MessageReceived_Event;
        #endregion

        enum keys : byte
        {
            PublicKey,
            PrivateKey, 
            SessionKey
        }

        public List<Chat> Connections = new List<Chat>();
        public Chat CurrentConnection;

        public Server(string localIp, ushort port)
        { 
            LOCAL_IP = localIp;
            PORT = port;
        }

        public void Connect(string ip)
        {
            tcpClient = new TcpClient(IPAddress.Parse(ip).AddressFamily);
            tcpClient.Connect(IPAddress.Parse(ip), PORT);
            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream());
            streamWriter.AutoFlush = true;

            CurrentConnection = new Chat();
            CurrentConnection.receiverIp = ip;
            Connections.Add(CurrentConnection);

            Console.WriteLine($"{DateTime.Now}[LOG]: Connected {CurrentConnection.receiverIp}");
            SendHandshake();
        }

        public async void SendMessage(string message)
        {
            await streamWriter.WriteLineAsync(message);
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

                if (!MessageHandler.IsHandshakeRequest(line, HANDSHAKE_HEADER))
                {
                    await Console.Out.WriteLineAsync("\n" + line);
                    MessageReceived_Event.Invoke(line);
                    //await Task.Delay(5);
                }
                else
                {
                    if (CurrentConnection == null)
                    {
                        if (true) //TODO verification that chat created or remove if
                        {
                            Connections.Add(new Chat());
                        }
                        if (CurrentConnection.RemotePublicKey == null)
                        {

                        }
                    }


                    switch ((byte)line[HANDSHAKE_HEADER.Length] - '0') //most efficient method
                    {
                        case (byte)keys.PublicKey: //must send session key encrypted with the received public key
                            CurrentConnection.RemotePublicKey2 = line.Substring(HANDSHAKE_HEADER.Length + 1);
                            CurrentConnection.GenerateAES(); 
                            Connections.Add(CurrentConnection);
                            SendHandshake(1);
                            break;

                        case (byte)keys.PrivateKey: //must continue handshake and send session key
                            CurrentConnection.RemotePublicKey = Encoding.UTF8.GetBytes(line.Substring(HANDSHAKE_HEADER.Length + 1, RSA_PRIVATE_KEY_LENGTH));
                            await Console.Out.WriteLineAsync();
                            break;

                        case (byte)keys.SessionKey: 
                            CurrentConnection.AESKey = Encoding.UTF8.GetBytes(line.Substring(HANDSHAKE_HEADER.Length + 2, AES_KEY_LENGTH));
                            CurrentConnection.AESIV  = Encoding.UTF8.GetBytes(line.Substring(HANDSHAKE_HEADER.Length + 2 + AES_KEY_LENGTH, AES_IV_LENGTH));
                            break;
                        //additional date with handshake header, like ip and public key 
                    }
                }
            }
        }

        private void SendHandshake(int stage = 0)
        {        
            switch (stage)
            {
                case 0:
                    SendMessage(HANDSHAKE_HEADER + (byte)keys.PublicKey + CurrentConnection.localPublicKey2);
                    break; 

                case 1:
                    SendMessage(HANDSHAKE_HEADER + (byte)keys.SessionKey +
                        Encoding.UTF8.GetString(MessageHandler.EncryptMessageAsync(CurrentConnection.AESKey, CurrentConnection.RemotePublicKey2)) +
                        Encoding.UTF8.GetString(MessageHandler.EncryptMessageAsync(CurrentConnection.AESIV, CurrentConnection.RemotePublicKey2)));
                    break;
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

        #region Encriprion and decription

        #region Encription
        public static string EncryptMessageSync(string message, Aes aes)
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message); // is ASCII better?

            return Encoding.UTF8.GetString(aes.EncryptEcb(byteMessage, PaddingMode.PKCS7));
        }

        public static byte[] EncryptMessageAsync(byte[] message, string keyXML)
        {
            RSA rsa = RSA.Create();
            rsa.FromXmlString(keyXML);

            return rsa.Encrypt(message, RSAEncryptionPadding.OaepSHA1);
        }
        #endregion

        #region Decription
        public static string DecryptMessageSync(string message, Aes aes) 
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);
            
            return Encoding.UTF8.GetString(aes.DecryptEcb(byteMessage, PaddingMode.PKCS7));
        }

        public static string DecryptMessageAsync(string message, RSAParameters key)
        {
            RSA rsa = RSA.Create();
            rsa.ImportParameters(key);
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);

            return Encoding.UTF8.GetString(rsa.Decrypt(byteMessage, RSAEncryptionPadding.OaepSHA1));
        }
        #endregion

        #endregion
    }
}
