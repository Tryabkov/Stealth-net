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
using System.Windows.Media;


namespace test_chat.MVVM.ViewModels
{
    class MainViewModel : Base.BaseVeiwModel
    {
        
        Server server;
        public string LocalIp_TextBlock { get => _localIp_TextBlock; set { _localIp_TextBlock = value; OnPropertyChanged(); } }
        private string? _localIp_TextBlock;

        public string ReceiverIp_TextBox { get => _receiverIp_TextBox; set { _receiverIp_TextBox = value; OnPropertyChanged(); } }
        private string _receiverIp_TextBox;
        public ObservableCollection<Message> Messages { get => _messages; set { _messages = value; OnPropertyChanged(); } }
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>() { new Message("aboba", true), new Message("aboba", false) };

        public string Main_TextBox { get => _main_TextBox; set { _main_TextBox = value; OnPropertyChanged(); } }
        private string _main_TextBox;

        public MainViewModel()
        { 
            SetIp();
            server = new Server(LocalIp_TextBlock);
            server.MessageReceived_Event += OnMessageReceived;
            server.MessageSent_Event += OnMessageSend;
            server.StartReceiving();
        }

        private void SetIp()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                LocalIp_TextBlock = endPoint.Address.ToString();
            }// checks IP address
        }

        private void OnMessageSend(string line)
        {
            Messages.Add(new Message(line, false));
        }

        private void OnMessageReceived(string line)
        {
            Messages.Add(new Message(line, true));
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

    public class Message
    {
        static readonly public SolidColorBrush ReceivedMessageColor = new SolidColorBrush(Color.FromArgb(255, 13, 13, 13));
        static readonly public SolidColorBrush ReceivedMessageTextColor = new SolidColorBrush(Color.FromArgb(255, 13, 13, 13));
        static readonly public SolidColorBrush SendMessageColor = new SolidColorBrush(Color.FromArgb(255, 252, 232, 3));
        static readonly public SolidColorBrush SendMessageColorTextColor = new SolidColorBrush(Color.FromArgb(255, 255, 252, 255));

        public string Text;
        public string Time;
        public SolidColorBrush Background;
        public SolidColorBrush Foreground;
        public Message(string text, bool isReceived)
        {
            Text = text;
            Time = $"{DateTime.Now.Hour} + {DateTime.Now.Minute}";
            if (isReceived)
            {
                Background = ReceivedMessageColor;
                Foreground = ReceivedMessageTextColor;
            }
            else
            {
                Background = SendMessageColor;
                Foreground = SendMessageColorTextColor;
            }
        }
    }
}
