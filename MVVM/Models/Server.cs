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


namespace test_chat.MVVM.Models
{
    internal class Server
    {
        const int PORT = 8888;
        TcpClient client;
        StreamReader streamReader;
        StreamWriter streamWriter;
        TcpListener listener;

        public delegate void AsyncEventHandler(string line);
        public event AsyncEventHandler? MessageReceived_Event;

        public void Connect(string ip)
        {
            client = new TcpClient(IPAddress.Parse(ip).AddressFamily); //
            client.Connect(IPAddress.Parse(ip), PORT);
            streamReader = new StreamReader(client.GetStream());
            streamWriter = new StreamWriter(client.GetStream());
            streamWriter.AutoFlush = true;
            Console.WriteLine($"{DateTime.Now}[LOG]: Connected");
        }

        public async void Send(string message)
        {
            await streamWriter.WriteLineAsync(message);
            await Console.Out.WriteLineAsync($"{DateTime.Now}[LOG]: send");
        }

        public async void StartReceiving()
        {
            listener = new TcpListener(IPAddress.IPv6Any, PORT);
            listener.Start();
            await Console.Out.WriteLineAsync($"{DateTime.Now}[LOG]: Started");
            var client = await listener.AcceptTcpClientAsync();

            while (client.Connected)
            {
                streamReader = new StreamReader(client.GetStream());
                var line = await streamReader.ReadLineAsync();
                await Console.Out.WriteLineAsync("\n" + line);
                MessageReceived_Event.Invoke(line);
                //await Task.Delay(10);
            }
        }
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
