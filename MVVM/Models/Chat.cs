using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EllipticCurve;

namespace test_chat.MVVM.Models
{
    public class Chat
    {
        private Guid ID { get; } = new Guid();

        public byte[] AESKey;
        public byte[] AESIV;

        public string localPrivateKey;
        public string localPublicKey;
        public string SharedPublicKey;

        public string localPrivateKeyCUR;
        public string localPublicKeyCUR;
        public string SharedPublicKeyCUR;

        public string receiverIp;

        public bool IsHandshakeCompleted;

        public Chat()
        {
            RSA rsa = RSA.Create(2048);
            localPrivateKey = rsa.ToXmlString(true);
            localPublicKey = rsa.ToXmlString(false);

            var privateKey = new PrivateKey();
            localPublicKeyCUR = privateKey.publicKey().toPem();
            localPrivateKeyCUR = privateKey.toPem();
        }

        public void GenerateAES()
        {
            var aes = Aes.Create();
            AESKey = aes.Key;
            AESIV = aes.IV;
        }
    }
}
