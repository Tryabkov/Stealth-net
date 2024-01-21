using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using test_chat.MVVM.Models;
using System.Windows.Input;
using System.Windows;
using System.IO;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Net.Sockets;


namespace test_chat.MVVM.ViewModels
{
    class MainViewModel : Base.BaseVeiwModel
    {
        Server server;
        public string LocalIp_TextBlock { get => _localIp_TextBlock; set { _localIp_TextBlock = value; OnPropertyChanged(); } }
        private string? _localIp_TextBlock;

        public string ReceiverIp_TextBox { get => _receiverIp_TextBox; set { _receiverIp_TextBox = value; OnPropertyChanged(); } }
        private string _receiverIp_TextBox;
        public ObservableCollection<string> Messages_ListBox { get => _messages_ListBox; set { _messages_ListBox = value; OnPropertyChanged(); } }
        private ObservableCollection<string> _messages_ListBox = new ObservableCollection<string>();

        public string Main_TextBox { get => _main_TextBox; set { _main_TextBox = value; OnPropertyChanged(); } }
        private string _main_TextBox;

        public MainViewModel()
        { 
            SetIp();
            server = new Server(LocalIp_TextBlock, 8888);
            server.MessageReceived_Event += MessageReceived;
            server.MessageSent_Event += MessageReceived;
            server.StartReceiving();
        }

        private void SetIp()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                LocalIp_TextBlock = endPoint.Address.ToString();
            }
        }

        private void MessageReceived(string line)
        {
            Messages_ListBox.Add(line);
        }

        public ICommand Connect_ButtonClick
        {
            get
            {
                return new DelegateCommand((obj) =>
                {
                    server.Connect(ReceiverIp_TextBox);
                });
            }
        }

        public ICommand UpdateIp_ButtonClick
        {
            get
            {
                return new DelegateCommand((obj) =>
                {
                    SetIp();
                });
            }
        }

        public ICommand Send_ButtonClick
        {
            get
            {
                return new DelegateCommand((obj) =>
                {
                    server.SendMessage(Main_TextBox);
                    Main_TextBox = "";
                });
            }
        }
    }
}
