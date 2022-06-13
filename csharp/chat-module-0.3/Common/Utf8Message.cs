using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Common
{
    using Interface;

    public class Utf8Message : IMessage
    {
        private string Str = "";
        private ReadOnlyMemory<byte> Bytes;

        public void SetString(string str)
        {
            Str = str;
            Bytes = Utf8PayloadProtocol.Encode(str);
        }

        public void SetBytes(byte[] bytes, int index, int count)
        {
            Str = Utf8PayloadProtocol.Decode(bytes, index, count);
            Bytes = bytes;
        }


        public ReadOnlySpan<byte> GetBytes()
        {
            return Bytes.Span;
        }

        public string GetString() 
        {
            return Str;
        }

        public override string ToString()
        {
            return GetString();
        }

        public string GetInfo()
        {
            return $"Utf8Message: {Str}, Bytes: {Convert.ToHexString(Bytes.Span)}";
        }
    }
}
