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

        #region cryptographic keys
        public byte[] AESKey;
        public byte[] AESIV;

        public string localPrivateKey;
        public string localPublicKey;
        public string SharedPublicKey;

        public string localCurvePrivateKey;
        public string localCurvePublicKey;
        public string SharedCurvePublicKey;
        #endregion
            
        public string receiverIp;

        public bool IsHandshakeCompleted;

        /// <summary>
        /// creates local pairs of RSA keys and Curve25519 keys
        /// </summary>
        public Chat()
        {
            RSA rsa = RSA.Create(2048);
            localPrivateKey = rsa.ToXmlString(true);
            localPublicKey = rsa.ToXmlString(false);

            var privateKey = new EllipticCurve.PrivateKey();
            localCurvePublicKey = privateKey.publicKey().toPem();
            localCurvePrivateKey = privateKey.toPem();
        }

        /// <summary>
        /// creates session key
        /// </summary>
        public void GenerateAES()
        {
            var aes = Aes.Create();
            AESKey = aes.Key;
            AESIV = aes.IV;
        }
    }
}
