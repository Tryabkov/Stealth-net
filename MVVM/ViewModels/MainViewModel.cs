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


namespace test_chat.MVVM.ViewModels
{
    class MainViewModel : Base.BaseVeiwModel
    {
        Server serv = new Server();
        public string Ip_TextBlock { get => _ip_TextBlock; set { _ip_TextBlock = value; OnPropertyChanged(); } }
        private string? _ip_TextBlock;

        public string ReceiverIp_TextBox { get => _receiverIp_TextBox; set { _receiverIp_TextBox = value; OnPropertyChanged(); } }
        private string _receiverIp_TextBox;
        public ObservableCollection<string> Messages_ListBox { get => _messages_ListBox; set { _messages_ListBox = value; OnPropertyChanged(); } }
        private ObservableCollection<string> _messages_ListBox;

        public string Main_TextBox { get => _main_TextBox; set { _main_TextBox = value; OnPropertyChanged(); } }
        private string _main_TextBox;

        public MainViewModel() 
        {          
            SetIp();
            serv.MessageReceived_Event += MessageReceived;
            serv.StartReceiving();
        }

        private void SetIp()
        {
            string strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            Ip_TextBlock = ipEntry?.AddressList[3].ToString();    
        }

        private void MessageReceived(StreamReader streamReader)
        {
            Messages_ListBox.Add(streamReader.ReadToEnd());
        }

        public ICommand Connect_ButtonClick
        {
            get
            {
                return new DelegateCommand((obj) =>
                {
                    serv.Connect(ReceiverIp_TextBox);
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
                    serv.Send(Main_TextBox);
                    Main_TextBox = "";
                });
            }
        }
    }
}
