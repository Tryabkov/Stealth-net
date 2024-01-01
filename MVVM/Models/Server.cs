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
        TcpListener tcpListener = new TcpListener(IPAddress.Any, 8888); // сервер для прослушивания

        public async Task ListenAsync()
        {
            try
            {
                tcpListener.Start();
                Console.WriteLine("Server is running. Waiting for connections...");

                while (true)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    await Console.Out.WriteLineAsync();
                    //ClientObject clientObject = new ClientObject(tcpClient, this);
                    //clients.Add(clientObject);
                    //Task.Run(clientObject.ProcessAsync);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                //Disconnect();
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
            _tcpClient = tcpClient;
            _server = server;

            var stream = tcpClient.GetStream();
            Reader = new StreamReader(stream);
            Writer = new StreamWriter(stream);
        }



    }
}
