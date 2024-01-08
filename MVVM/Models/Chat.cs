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

        public byte[] AESKey;
        public byte[] AESIV;

        public string localPrivateKey2;
        public string localPublicKey2;
        public string RemotePublicKey2;

        public string receiverIp;

        public bool IsHandshakeCompleted;

        public Chat()
        {
            RSA rsa = RSA.Create(2048);
            localPrivateKey2 = rsa.ToXmlString(true);
            localPublicKey2 = rsa.ToXmlString(false);
        }

        public void GenerateAES()
        {
            var aes = Aes.Create();
            AESKey = aes.Key;
            AESIV = aes.IV;
        }
    }
}
