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
        const int AES_KEY_LENGTH = 32;
        const int AES_IV_LENGTH = 16;

        const int RSA_PUBLIC_KEY_LENGTH = 415;
        const int RSA_PRIVATE_KEY_LENGTH = 1679;

        const int CURVE_PUBLIC_KEY_LENGTH = 17;
        const int CURVE_PRIVATE_KEY_LENGTH = 218;

        const string HANDSHAKE_HEADER = "00000handshake initiation00000";
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
        public event AsyncEventHandler? MessageSent_Event;
        #endregion

        enum KeysId : byte
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

        /// <summary>
        /// Builds connection and sends handshake if it necessary.
        /// </summary>
        /// <param name="ip">Receiver's IP.</param>
        /// <param name="isHandshakeRequired">True if handshake isn't done</param>
        public void Connect(string ip, bool isHandshakeRequired = true)
        {
            tcpClient = new TcpClient(IPAddress.Parse(ip).AddressFamily);
            tcpClient.Connect(IPAddress.Parse(ip), PORT);
            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream());
            streamWriter.AutoFlush = true; //building connection

            CurrentConnection = new Chat();
            CurrentConnection.receiverIp = ip;
            Connections.Add(CurrentConnection);

            Console.WriteLine($"{DateTime.Now}[LOG]: Connected {CurrentConnection.receiverIp}");
            if (isHandshakeRequired) { SendHandshake(); }
        }

        /// <summary>
        /// Asynchronously sends text string to receiver's IP.  
        /// </summary>
        /// <param name="message">The string to send.</param>
        /// <param name="sendEvent"></param>
        public async void SendMessage(string message, bool sendEvent = true)
        {
            await streamWriter.WriteLineAsync(message + LOCAL_IP);
            await Console.Out.WriteLineAsync($"{DateTime.Now}[LOG]: Send");
            if (sendEvent) { MessageSent_Event?.Invoke(message); }
        }

        /// <summary>
        /// Starts line receiving and handles handshakes.
        /// </summary>
        public async void StartReceiving()
        {
            listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            await Console.Out.WriteLineAsync($"{DateTime.Now}[LOG]: Started");
            var client = await listener.AcceptTcpClientAsync();

            while (client.Connected)
            {
                streamReader = new StreamReader(client.GetStream());
                var line = await streamReader.ReadLineAsync(); //line - not handled message

                if (!MessageHandler.IsHandshakeRequest(line, HANDSHAKE_HEADER)) //Substrings received line and sends event
                {
                    string msg = line.Substring(0, line.Length - LOCAL_IP.Length);
                    await Console.Out.WriteLineAsync("\n" + msg);
                    MessageReceived_Event?.Invoke(msg);
                    //await Task.Delay(5);
                }
                else
                {
                    switch ((byte)line[HANDSHAKE_HEADER.Length] - '0') //Most efficient method
                    {
                        case (byte)KeysId.PublicKey: //Sends session key encrypted with the received public key
                            Connect(ip: line.Substring(HANDSHAKE_HEADER.Length + 1 + CURVE_PUBLIC_KEY_LENGTH), isHandshakeRequired: false); //Extract IP from line
                            CurrentConnection.SharedCurvePublicKey = line.Substring(HANDSHAKE_HEADER.Length + 1, CURVE_PUBLIC_KEY_LENGTH);  //Extract public key from line

                            CurrentConnection.GenerateAES(); 
                            SendHandshake(stage: 2);
                            break;

                        case (byte)KeysId.SessionKey: 
                            CurrentConnection.AESKey = Encoding.UTF8.GetBytes(line.Substring(HANDSHAKE_HEADER.Length + 2, AES_KEY_LENGTH));
                            CurrentConnection.AESIV  = Encoding.UTF8.GetBytes(line.Substring(HANDSHAKE_HEADER.Length + 2 + AES_KEY_LENGTH, AES_IV_LENGTH));
                            break;
                        //Additional date with isHandshakeRequired header, like ip and public key ??
                    }
                }
            }
        }

        /// <summary>
        /// Sends handshakes:
        /// <list>
        /// <item>Stage 1: Sends local public key.</item>
        /// <item>Stage 2: Sends session key, encrypted by shared key.</item>
        /// </list>
        /// </summary>
        /// <param name="stage"></param>
        private void SendHandshake(int stage = 1)
        {        
            switch (stage)
            {
                case 1: //send local public key
                    SendMessage(HANDSHAKE_HEADER + (byte)KeysId.PublicKey + CurrentConnection.localPublicKey, sendEvent: false);
                    break; 

                case 2: //send session key, encrypted by shared key
                    SendMessage(HANDSHAKE_HEADER + (byte)KeysId.SessionKey +
                        Encoding.UTF8.GetString(MessageHandler.EncryptMessageAsymmetric(CurrentConnection.AESKey, CurrentConnection.SharedCurvePublicKey)) +
                        Encoding.UTF8.GetString(MessageHandler.EncryptMessageAsymmetric(CurrentConnection.AESIV, CurrentConnection.SharedCurvePublicKey)), sendEvent: false);
                    break;
            }
        }
    }

    public static class MessageHandler
    {
        /// <summary>
        /// Checks if the line starts with a handshake.
        /// </summary>
        /// <param name="msg">Message to check.</param>
        /// <param name="HSHeader">Handshake.</param>
        /// <returns></returns>
        public static bool IsHandshakeRequest(string msg, string HSHeader)
        {
            if (msg.Length >= HSHeader.Length)
            {
                for (int i = 0; i < HSHeader.Length; i++)
                {
                    if (!(msg[i] == HSHeader[i])) { break; }
                }
                return true;
            }
            return false;
        }

        #region Encriprion and decription

        #region Encription
        public static string EncryptMessageSymmetric(string message, Aes aes)
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message); // is ASCII better?

            return Encoding.UTF8.GetString(aes.EncryptEcb(byteMessage, PaddingMode.PKCS7));
        }

        public static byte[] EncryptMessageAsymmetric(byte[] message, string keyXML)
        {
            RSA rsa = RSA.Create();
            rsa.FromXmlString(keyXML);

            EllipticCurve.PrivateKey privateKey = new EllipticCurve.PrivateKey();


            return rsa.Encrypt(message, RSAEncryptionPadding.OaepSHA256);
        }
        #endregion

        #region Decription
        public static string DecryptMessageSymmetric(string message, Aes aes) 
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message);

            return Encoding.UTF8.GetString(aes.DecryptEcb(byteMessage, PaddingMode.PKCS7));
        }

        public static string DecryptMessageAsymmetric(string message, RSAParameters key)
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
