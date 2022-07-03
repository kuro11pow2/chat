using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;


namespace Common
{
    using Utility;
    using Interface;

    public class Utf8Packet : IPacket
    {
        private string Str = "";
        private Memory<byte> FullBytes;

        public void Set(Message message)
        {
            Set(Serializer<Message>.Serialize(message));
        }

        public void Set(string str)
        {
            Str = str;
            FullBytes = Utf8PayloadProtocol.Encode(str);
        }

        public void Set(byte[] fullBytes, int validBytesLength)
        {
            Str = Utf8PayloadProtocol.Decode(fullBytes, validBytesLength);
            FullBytes = new Memory<byte>(fullBytes, 0, validBytesLength);
        }


        public Memory<byte> GetFullBytes()
        {
            return FullBytes;
        }

        public int GetFullBytesLength()
        {
            return FullBytes.Length;
        }

        public string GetRawString() 
        {
            return Str;
        }

        public string GetInfo()
        {
            return $"Utf8Packet: {GetRawString()}, Bytes: {Convert.ToHexString(FullBytes.ToArray())}, BytesLength: {GetFullBytesLength()}";
        }
    }
}
