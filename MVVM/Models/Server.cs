using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace test_chat.MVVM.Models
{
    internal class Server
    {
        TcpClient client;
        StreamReader streamReader;
        StreamWriter streamWriter;

        TcpListener listener;

        
        public void Connect(string ip)
        {
            client = new TcpClient();
            client.Connect(IPAddress.Parse(ip), 8888);
            streamReader = new StreamReader(client.GetStream());
            streamWriter = new StreamWriter(client.GetStream());
            streamWriter.AutoFlush = true;
            Console.WriteLine("Connected");
        }

        public async void Send()
        {
            await streamWriter.WriteLineAsync($"Hello");
            await Console.Out.WriteLineAsync("send");
        }

        public async void StartReceiving()
        {
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
            await Console.Out.WriteLineAsync("Started");

            var client = await listener.AcceptTcpClientAsync();
            while (true)
            {
                var line = await streamReader.ReadLineAsync();
                await Console.Out.WriteLineAsync("Received");
                await Console.Out.WriteLineAsync(line?.ToString());
                await Task.Delay(100);
                
            }

        }



        //public async void OpenSocket()
        //{
        //    

        //    Socket socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //    socketListener.Bind(endPoint);
        //    socketListener.Listen(1000);
        //    Console.WriteLine(socketListener.LocalEndPoint);

        //    await socketListener.AcceptAsync();
        //}

        //public async void Connect(string ip, string message = "Hello")
        //{
        //    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //    await socket.ConnectAsync(IPAddress.Parse(ip), 8888);
        //    await Console.Out.WriteLineAsync($"Successful connection to {ip}:8888");
        //}
    }
    
    public class Client
    {
        private Guid ID { get; } = new Guid();
        private StreamWriter Writer { get; }
        private StreamReader Reader { get; }

        private TcpClient _tcpClient;
        private Server _server;

        Client(TcpClient tcpClient, Server server) 
        {
            
        }



    }
}
