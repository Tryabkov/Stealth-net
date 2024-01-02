using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using test_chat.MVVM.Models;
using System.Windows.Input;
using System.Windows;


namespace test_chat.MVVM.ViewModels
{
    class MainViewModel : Base.BaseVeiwModel
    {
        Server serv = new Server();
        public string Ip_TextBlock { get => _ip_TextBlock; set { _ip_TextBlock = value; OnPropertyChanged(); } }
        private string? _ip_TextBlock;

        public string ReceiverIp_TextBox { get => _receiverIp_TextBox; set { _receiverIp_TextBox = value; OnPropertyChanged(); } }
        private string _receiverIp_TextBox;

        public MainViewModel() 
        {
            SetIp();
            serv.StartReceiving();
        }

        private void SetIp()
        {
            string strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            Ip_TextBlock = ipEntry?.AddressList[3].ToString();    
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
                    serv.Send();
                });
            }
        }
    }
}
