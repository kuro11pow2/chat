using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    using Utility;

    public class Packet
    {
        private const int PacketHeaderLength = 1;
        private byte[] _packetBytes;
        private string? _message;

        public Packet(string message) : this(String2PacketBytes(message))
        {
            _message = message;
        }

        public Packet(byte[] packetBytes)
        {
            _packetBytes = packetBytes;
        }

        public string Message
        {
            get 
            { 
                if (string.IsNullOrEmpty(_message)) 
                    _message = PacketBytes2String(_packetBytes);
                return _message;
            }
        }

        public byte[] PacketBytes
        {
            get { return _packetBytes; }
        }

        private static byte[] String2PacketBytes(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            int messageBytesLength = messageBytes.Length;

            byte[] sizeBytes = BitConverter.GetBytes(messageBytesLength)[..PacketHeaderLength];
            byte[] fullBytes = new byte[sizeBytes.Length + messageBytesLength];

            Buffer.BlockCopy(sizeBytes, 0, fullBytes, 0, sizeBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, fullBytes, sizeBytes.Length, messageBytes.Length);

            return fullBytes;
        }

        private string PacketBytes2String(byte[] bytes)
        {
            ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>(bytes);
            ReadOnlySpan<byte> body = buffer.Slice(PacketHeaderLength);

            string message = Encoding.UTF8.GetString(body);

            return message;
        }
    }
}
