﻿using System;
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
        const int RSA_PUBLIC_KEY_LENGTH = 415;
        const int RSA_PRIVATE_KEY_LENGTH = 1679;
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

        enum Keys : byte
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

        public void Connect(string ip, bool isHandshakeRequired = true)
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
            if (isHandshakeRequired) { SendHandshake(); }
        }

        public async void SendMessage(string message, bool sendEvent = true)
        {
            await streamWriter.WriteLineAsync(message + LOCAL_IP);
            await Console.Out.WriteLineAsync($"{DateTime.Now}[LOG]: Send");
            if (sendEvent) { MessageSent_Event.Invoke(message); }
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
                    string msg = line.Substring(line.Length - LOCAL_IP.Length);
                    await Console.Out.WriteLineAsync("\n" + msg);
                    MessageReceived_Event.Invoke(msg);
                    //await Task.Delay(5);
                }
                else
                {
                    switch ((byte)line[HANDSHAKE_HEADER.Length] - '0') //most efficient method
                    {
                        case (byte)Keys.PublicKey: //must send session key encrypted with the received public key
                            Connect(line.Substring(HANDSHAKE_HEADER.Length + 1 + RSA_PUBLIC_KEY_LENGTH), isHandshakeRequired: false);
                            CurrentConnection.SharedPublicKey = line.Substring(HANDSHAKE_HEADER.Length + 1, RSA_PUBLIC_KEY_LENGTH);
                            CurrentConnection.GenerateAES(); 
                            SendHandshake(1);
                            break;

                        case (byte)Keys.SessionKey: 
                            CurrentConnection.AESKey = Encoding.UTF8.GetBytes(line.Substring(HANDSHAKE_HEADER.Length + 2, AES_KEY_LENGTH));
                            CurrentConnection.AESIV  = Encoding.UTF8.GetBytes(line.Substring(HANDSHAKE_HEADER.Length + 2 + AES_KEY_LENGTH, AES_IV_LENGTH));
                            break;
                        //additional date with isHandshakeRequired header, like ip and public key 
                    }
                }
            }
        }

        private void SendHandshake(int stage = 0)
        {        
            switch (stage)
            {
                case 0:
                    SendMessage(HANDSHAKE_HEADER + (byte)Keys.PublicKey + CurrentConnection.localPublicKey2, sendEvent: false);
                    break; 

                case 1:
                    SendMessage(HANDSHAKE_HEADER + (byte)Keys.SessionKey +
                        Encoding.UTF8.GetString(MessageHandler.EncryptMessageAsync(CurrentConnection.AESKey, CurrentConnection.SharedPublicKey)) +
                        Encoding.UTF8.GetString(MessageHandler.EncryptMessageAsync(CurrentConnection.AESIV, CurrentConnection.SharedPublicKey)), sendEvent: false);
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

            return rsa.Encrypt(message, RSAEncryptionPadding.OaepSHA256);
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
