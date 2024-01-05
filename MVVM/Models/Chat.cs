using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace test_chat.MVVM.Models
{
    public class Chat
    {
        private Guid ID { get; } = new Guid();

        private Aes aes;
        private RSAParameters localPrivateKey { get; }
        private RSAParameters localPublicKey { get; }
        private RSAParameters RemotePublicKey { get; set; }
        public string receiverIp { get; }

        public Chat(string ReceiverIp)
        {
            receiverIp = ReceiverIp;

            RSA rsa = RSA.Create();
            localPrivateKey = rsa.ExportParameters(true);
            localPublicKey = rsa.ExportParameters(false);
        }

        public void GenerateAES()
        {
            aes = Aes.Create();
        }
    }
}
